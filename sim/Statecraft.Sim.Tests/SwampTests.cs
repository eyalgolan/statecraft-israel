using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Dry swamp (GDD Phase 4 PR 5; GDD "Pioneer work", "Dry swamp"): PioneerWork(dry_swamp) at a swamp site, which
/// drained becomes a field site (R-SWAMP-001), and an event tied to a site by `site:`, which fires only while that site is
/// being worked (R-SWAMP-002). The owner took the plan's recommendations on #782 (17 October 2026): the Hula only, a field
/// once drained (Q-SWAMP-1), the event only while the works are in progress (Q-SWAMP-2); and "Allow as planned" for the
/// Hula on #789 (Q-SWAMP-3).</summary>
public class SwampTests
{
    /// <summary>A swamp site, marsh, and its field site, marsh_field, in a hex of their own (no yishuv there, so only the
    /// swamp gates the field). Fixture values, not history.</summary>
    const string Marsh = @"
  - id: marsh
    name: Marsh
    name_he: ביצה
    q: 3
    r: 0
    kinds: [swamp]
    sources:
      - { url: ""https://example.org/marsh"", checked: 2026-10-14 }
    status: draft
  - id: marsh_field
    name: Marsh fields
    name_he: שדות הביצה
    q: 3
    r: 0
    kinds: [field]
    sources:
      - { url: ""https://example.org/marsh"", checked: 2026-10-14 }
    status: draft
";

    /// <summary>An event about the works at <paramref name="site"/>, in its window from the game's start.</summary>
    static string Works(string site) => $@"
id: works
title: The works
text: 'The drainage works reach the zone.'
from: 1948-05-15
site: {site}
options:
  - id: go_on
    text: Go on
    historical: true
";

    static Dictionary<string, string> Files() => Fixture.Gdd(new Dictionary<string, string> { ["sites.yaml"] = Fixture.GddSites + Marsh });

    /// <summary>The GDD fixture with the marsh: draining takes dry_swamp_turns 3 and costs IL 2m (fixture values).</summary>
    static Content Town(string site = "marsh") => ContentLoader.Load(Fixture.Write(Files(), new Dictionary<string, string> { ["works.yaml"] = Works(site) }));

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static GameState WithHalutz(Content c, out int id)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.FiredEvents.Add("works");   // keep the fixture's event out of the way of the work tests
        id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", 1, 0));
        return s;
    }

    static PioneerWork Drain(int unit, string site = "marsh") => new PioneerWork(unit, Projects.DrySwamp, site);

    // R-SWAMP-002 (Review focus 5): in its window, with no works, the event does not fire; with works in progress it does;
    // once the swamp is drained the works are over and it does not.
    [Fact]
    public void TheHulaEventNeedsWorksInProgress()
    {
        var c = Town();
        var none = new GameState { Date = new DateTime(1948, 6, 1), RngState = 3 };
        EffectsSystem.Init(c, none); EconomySystem.Init(c, none);
        var working = none.Clone();
        working.Units.Add(99, new Unit(99, "halutz", 3, 0));
        working.Sites["marsh"] = new SiteWork("marsh", 99, 2, null);
        var drained = none.Clone();
        drained.Sites["marsh"] = SiteWork.Built("marsh", new DateTime(1948, 5, 15));

        EventSystem.Trigger(c, none, new List<Cause>());
        EventSystem.Trigger(c, working, new List<Cause>());
        EventSystem.Trigger(c, drained, new List<Cause>());
        Assert.DoesNotContain("works", none.PendingEvents);
        Assert.Contains("works", working.PendingEvents);
        Assert.DoesNotContain("works", drained.PendingEvents);
        Assert.DoesNotContain("works", none.FiredEvents);   // not spent: it can still fire once works start in its window
    }

    // R-SWAMP-002, through the turn: the order starts the works and the same turn's event check sees them.
    [Fact]
    public void TheEventFiresOnTheTurnTheWorksStart()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", 1, 0));
        Assert.DoesNotContain("works", Step(c, s).State.PendingEvents);
        Assert.Contains("works", Step(c, s, Drain(id)).State.PendingEvents);
    }

    // R-SWAMP-002: the legacy loop reads no sites.yaml and ignores the key, so the event fires as it did (the legacy goldens hold).
    [Fact]
    public void TheLegacyLoopIgnoresTheSiteKey()
    {
        var c = ContentLoader.Load(Fixture.Write(events: new Dictionary<string, string> { ["works.yaml"] = Works("marsh") }));
        Assert.False(c.IsGdd);
        Assert.Equal("marsh", c.Events.Single(e => e.Id == "works").Site);
        var s = new GameState { Date = new DateTime(1948, 6, 1), RngState = 3 };
        EffectsSystem.Init(c, s); EconomySystem.Init(c, s);
        EventSystem.Trigger(c, s, new List<Cause>());
        Assert.Contains("works", s.PendingEvents);
    }

    // R-SWAMP-001 (Q-SWAMP-1): draining follows R-SITE-001 to R-SITE-003 (paid in full at the start, its turns counting the
    // order's); drained, the swamp's hex has a ready field site, which before that is refused with the state unchanged.
    [Fact]
    public void ADrainedSwampBecomesAField()
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        var early = Step(c, s, new PioneerWork(id, Projects.PlowFields, "marsh_field"));
        Assert.Equal("Marsh fields can be plowed once Marsh is drained", Assert.Single(early.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), early.State.Hash());

        var r = Step(c, s, Drain(id));
        Assert.Empty(r.Rejected);
        Assert.Equal(("marsh", (int?)id, 2), (r.State.Sites["marsh"].Site, r.State.Sites["marsh"].Unit, r.State.Sites["marsh"].TurnsLeft));
        Assert.Equal(Step(c, s).State.Treasury - 2, r.State.Treasury, 9);
        Assert.Contains(r.Why, x => x.System == "map" && x.Message == "Marsh: a group of halutzim began draining the swamp, cost IL 2.0m");

        var mid = Step(c, r.State);
        Assert.Null(mid.State.Sites["marsh"].BuiltOn);
        var done = Step(c, mid.State);
        Assert.Equal(done.State.Date, done.State.Sites["marsh"].BuiltOn);   // the end of the order's turn + 2
        Assert.False(Projects.IsBusy(done.State, id));
        Assert.Contains(done.Why, x => x.System == "map" && x.Message == "Marsh drained");
        Assert.False(done.State.Sites.ContainsKey("marsh_field"));   // drained is not plowed: the field yields nothing yet

        var plow = Step(c, done.State, new PioneerWork(id, Projects.PlowFields, "marsh_field"));
        Assert.Empty(plow.Rejected);
        Assert.True(plow.State.Sites.ContainsKey("marsh_field"));
    }

    // R-SITE-004, R-SWAMP-001: each action only at its own kind of site.
    [Theory]
    [InlineData("dry_swamp", "site_a", "Site A is not a swamp")]
    [InlineData("dry_swamp", "marsh_field", "Marsh fields is not a swamp")]
    [InlineData("build_yishuv", "marsh", "Marsh is not a site for a new yishuv")]
    public void DrainingOnlyAtASwampSite(string action, string site, string reason)
    {
        var c = Town();
        var s = WithHalutz(c, out var id);
        var r = Step(c, s, new PioneerWork(id, action, site));
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-SWAMP-002: in the GDD loop an event's site must be a swamp site of sites.yaml. R-SWAMP-001: a swamp site is not
    // also a field site (drained, it would yield Food unplowed), nor a yishuv or camp site (its build would drain the swamp),
    // and the drain values are required in the GDD loop, so a missing key is never read as 0.
    [Theory]
    [InlineData("site nope", "events/works.yaml", "unknown site 'nope'")]
    [InlineData("site site_a", "events/works.yaml", "site 'site_a' is not a swamp site")]
    [InlineData("kinds field", "sites.yaml", "a field site cannot also be a swamp site")]
    [InlineData("kinds yishuv", "sites.yaml", "a swamp site cannot also be a yishuv site")]
    [InlineData("kinds military_camp", "sites.yaml", "a swamp site cannot also be a military_camp site")]
    [InlineData("kinds hakhshara", "sites.yaml", "a swamp site cannot also be a hakhshara site")]
    [InlineData("dry_swamp_turns", "rules.yaml", "mvp.dry_swamp_turns: required")]
    [InlineData("dry_swamp_cost", "rules.yaml", "mvp.dry_swamp_cost: required")]
    public void BadSwampContentFailsLoad(string what, string file, string message)
    {
        var files = Files();
        var site = what.StartsWith("site ") ? what.Substring(5) : "marsh";
        if (what.StartsWith("kinds ")) files["sites.yaml"] = files["sites.yaml"].Replace("kinds: [swamp]", $"kinds: [swamp, {what.Substring(6)}]");
        else if (!what.StartsWith("site ")) files["rules.yaml"] = files["rules.yaml"].Replace(what == "dry_swamp_turns" ? "  dry_swamp_turns: 3\n" : "  dry_swamp_cost: 2\n", "");
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files, new Dictionary<string, string> { ["works.yaml"] = Works(site) })));
        Assert.Equal(file, e.File);
        Assert.Contains(message, e.Message);
    }

    // R-SWAMP-002 on the real content (owner review on #968): with the works in progress, the fact-checked card still fires on
    // its historical turn. The reference player to 1 February 1951, then a halutz ordered to drain the Hula alongside the
    // reference orders: the order lands on the turn ending 15 March 1951, and so does the card.
    [Fact]
    public void TheRealHulaCardFiresOnItsTurnWithWorksInProgress()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = GoldenRun.Play(c, new DateTime(1951, 2, 1));
        Assert.DoesNotContain("hula_drainage", s.FiredEvents);
        // The reference player's orders come first, so it does not hand the new group one of its own builds.
        var orders = GoldenRun.Orders(c, s);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", 14, 3));
        var r = Step(c, s, orders.Append(Drain(id, "hula")).ToArray());
        Assert.Empty(r.Rejected);
        Assert.Equal(new DateTime(1951, 3, 15), r.State.Date);
        Assert.Contains("hula_drainage", r.State.FiredEvents);
    }

    // The real content: the Hula is the one swamp site, in the swamp hex of the water research (14, 3), with one field site in
    // its hex; hula_drainage waits for its works; the design values are Q-SWAMP-1's (12 turns, IL 4m).
    [Fact]
    public void TheRealSwampIsThePlannedOne()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var swamp = Assert.Single(c.Sites, x => x.Kinds.Contains(Projects.SwampKind));
        Assert.Equal(("hula", 14, 3), (swamp.Id, swamp.Q, swamp.R));
        var field = Assert.Single(c.Sites, x => x.Kinds.Contains(Projects.FieldKind) && (x.Q, x.R) == (14, 3));
        Assert.Equal("hula_field", field.Id);
        Assert.Equal("hula", c.Events.Single(e => e.Id == "hula_drainage").Site);
        var m = c.Rules.Mvp!;
        Assert.Equal((12, 4.0), (m.DrySwampTurns!.Value, m.DrySwampCost!.Value));
    }
}
