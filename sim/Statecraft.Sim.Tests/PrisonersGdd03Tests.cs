using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Statecraft.Sim;

/// <summary>Prisoner returns under gdd-0.3 (#1710): the return-only scoring main already has (R-POW-004), pinned under the
/// gdd-0.3 identity. The exchange card is synthetic, built here: the real per-front cards wait on #751. Every capture is a
/// real surrender from a player's attack order, so a save, which is a command log, replays it. The fixture starts four
/// Israeli units of 6,000 people and two Egyptian units of 40, each Egyptian unit in a corner of the board with both its
/// land neighbours held by Israeli units. One Israeli unit attacks (a gdd-0.3 attack commits one adjacent unit): far above
/// 2:1, the defender loses one population unit and, with nowhere to retreat, surrenders with 3 (R-COMBAT-003). Neither
/// Egyptian unit attacks (far below 2:1) or marches (one anchor).</summary>
public class PrisonersGdd03Tests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";

    /// <summary>The starting forces, by id: 0 and 1 hold the land around unit 2 in corner (0, 0), 3 and 4 the land around unit
    /// 5 in corner (5, 2). Units 0 and 3 attack.</summary>
    static readonly (string Side, int People, int Q, int R)[] Forces =
    {
        ("israel", 6000, 1, 0), ("israel", 6000, 0, 1), ("egypt", 40, 0, 0),
        ("israel", 6000, 4, 2), ("israel", 6000, 5, 1), ("egypt", 40, 5, 2),
    };

    /// <summary>The synthetic exchange card for the Egyptian front, open from 1 June to 1 September 1948.</summary>
    const string Card = @"
id: pow_egypt
title: Prisoners exchanged
text: The prisoners are exchanged.
anchor: hard
prisoners: egypt
from: 1948-06-01
to: 1948-09-01
options:
  - id: continue
    text: Continue
    historical: true
";

    /// <summary>The fixture under the gdd-0.3 identity, with the real content's points_per_prisoner_unit_returned line in
    /// place of the fixture's, so the real value is the one played.</summary>
    static Content Load()
    {
        var rows = string.Concat(Forces.Select((f, i) => FormattableString.Invariant(
            $"- id: force_{i}\n  service: army\n  side: {f.Side}\n  status: draft\n  strength: {{ value: {f.People}, as_of: '1948-05-15' }}\n  anchors:\n  - {{ place: Here, lon: 34.3, lat: 33.3, from: '1948-05-15', to: '1957-01-01' }}\n")));
        var hexes = string.Concat(Forces.Select((f, i) => FormattableString.Invariant($"  - {{ id: force_{i}, anchor: 0, q: {f.Q}, r: {f.R} }}\n")));
        var files = Fixture.Gdd(new Dictionary<string, string>
        {
            ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n",
            ["map/units_1948.yaml"] = "units:\n" + rows, ["map/unit_anchor_hexes.yaml"] = "anchors:\n" + hexes,
        });
        NoAttentionTests.Select(files, true);
        var real = Regex.Match(File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "rules.yaml")), @"points_per_prisoner_unit_returned: \d+").Value;
        Assert.NotEqual("", real);
        Assert.Contains("points_per_prisoner_unit_returned: 7", files["rules.yaml"]);
        files["rules.yaml"] = files["rules.yaml"].Replace("points_per_prisoner_unit_returned: 7", real);
        var c = ContentLoader.Load(Fixture.Write(files, new Dictionary<string, string> { ["pow_egypt.yaml"] = Card }));
        Assert.Equal(SaveGame.Gdd03Ruleset, c.Ruleset);
        return c;
    }

    static Command[] Attack(int unit, int q, int r) => new Command[] { new UnitOrder(unit, Combat.Attack, q, r) };

    /// <summary>Unit 2 surrenders on the first turn (R-POW-001); empty turns until the card waits, which it then resolves.</summary>
    static void CaptureAndExchange(GameSession g)
    {
        Assert.Empty(g.Step(Attack(0, 0, 0)).Rejected);
        Assert.Equal(new[] { ("egypt", 3) }, g.State.PrisonersHeld.Select(kv => (kv.Key, kv.Value)));
        Assert.Equal(0, g.State.Score);
        for (int i = 0; i < 20 && !g.State.PendingEvents.Contains("pow_egypt"); i++) g.Step(Array.Empty<Command>());
        Assert.Contains("pow_egypt", g.State.PendingEvents);
        Assert.Empty(g.Step(new Command[] { new ChooseEventOption("pow_egypt", "continue") }).Rejected);
    }

    /// <summary>Unit 5 surrenders after the card has resolved, while its window is still open.</summary>
    static void CaptureAfterTheCard(GameSession g)
    {
        Assert.Empty(g.Step(Attack(3, 5, 2)).Rejected);
        Assert.False(g.State.Units.ContainsKey(5));
        Assert.True(g.State.Date < new DateTime(1948, 9, 1));
        Assert.Equal(new[] { ("egypt", 3) }, g.State.PrisonersHeld.Select(kv => (kv.Key, kv.Value)));
    }

    /// <summary>Plays empty turns to the end of the game (the era's end), checking after each that the score and the counts
    /// never move.</summary>
    static void PlayToTheEnd(GameSession g, int score)
    {
        while (g.State.GameOver == null)
        {
            g.Step(Array.Empty<Command>());
            Assert.Equal(score, g.State.Score);
            Assert.Equal(new[] { ("egypt", 3) }, g.State.PrisonersHeld.Select(kv => (kv.Key, kv.Value)));
            Assert.Equal(new[] { ("egypt", 3) }, g.State.PrisonersReturned.Select(kv => (kv.Key, kv.Value)));
        }
        Assert.Equal(GameOverCodes.EraEnded, g.State.GameOver);   // 1 January 1957, long after the card's window
    }

    // R-POW-004 under gdd-0.3. docs/gdd/military-combat.md:77: "A returned enemy population unit scores 10 points, main's
    // value (`R-POW-004`; owner, 1 November 2026, O7: "10 per unit, main (Recommended)") [S21]." Three units returned score 30,
    // all of it prisoner points (R-SCORE-001), and nothing scored at the capture.
    [Fact]
    public void AReturnedUnitScoresTenPoints()
    {
        var g = GameSession.New(Load(), 1, Mode.Historical);
        CaptureAndExchange(g);
        Assert.Equal(30, g.State.Score);
        Assert.Equal(30, ScoreParts.For(g.State).PrisonerPoints);
        Assert.Empty(g.State.PrisonersHeld);
        Assert.Equal(new[] { ("egypt", 3) }, g.State.PrisonersReturned.Select(kv => (kv.Key, kv.Value)));
    }

    // 27M-19 under gdd-0.3. docs/gdd/military-combat.md:77: "The retained return-only rule controls: points are scored only
    // when a front's exchange card returns prisoners, and prisoners taken after that front's card has resolved stay held to
    // the end, with no points (27M-19) [S21]." The card fires once (EventSystem.Trigger skips a fired event), so prisoners
    // taken inside its still-open window stay held to the end of the game and never score.
    [Fact]
    public void PrisonersTakenAfterTheCardStayHeldWithNoPoints()
    {
        var g = GameSession.New(Load(), 1, Mode.Historical);
        CaptureAndExchange(g);
        CaptureAfterTheCard(g);
        PlayToTheEnd(g, 30);
        Assert.Contains("pow_egypt", g.State.FiredEvents);
    }

    // R-POW-004 under gdd-0.3, through a save. docs/gdd/military-combat.md:106: "Combat never converts residents into
    // casualties, prisoners, workers or score; return bonuses pay once." A save made while the card waits pays once when
    // the loaded game resolves it; a save made after a later capture loads on the same state and never pays again.
    [Fact]
    public void ASaveAndReloadCannotPayTwice()
    {
        var c = Load();
        var g = GameSession.New(c, 1, Mode.Historical);
        Assert.Empty(g.Step(Attack(0, 0, 0)).Rejected);
        for (int i = 0; i < 20 && !g.State.PendingEvents.Contains("pow_egypt"); i++) g.Step(Array.Empty<Command>());
        var waiting = GameSession.Load(c, g.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Contains("pow_egypt", waiting.State.PendingEvents);
        Assert.Empty(waiting.Step(new Command[] { new ChooseEventOption("pow_egypt", "continue") }).Rejected);
        Assert.Equal(30, waiting.State.Score);

        Assert.Empty(g.Step(new Command[] { new ChooseEventOption("pow_egypt", "continue") }).Rejected);
        CaptureAfterTheCard(g);
        var loaded = GameSession.Load(c, g.Save.Write(), out warnings);
        Assert.Empty(warnings);
        Assert.Equal(g.State.Hash(), loaded.State.Hash());
        Assert.Equal(30, loaded.State.Score);
        PlayToTheEnd(loaded, 30);
    }
}
