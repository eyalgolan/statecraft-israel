using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1709 slice A, R-WAR-005 to R-WAR-007 under exact gdd-0.3: each front's count of quiet turns, the early offer's
/// eligibility and the removal of a front's units when its armistice is signed. The rulings: docs/gdd/military-combat.md:69
/// ("A quiet turn has no engagement involving that front's units, whoever attacked; a mixed-front engagement breaks the count
/// for every front in it, and a refused order does not break it. Two full quiet turns must follow a refusal", owner, 1 November
/// 2026, Q48 and Q49), the owner's 27 October 2026 ruling on #1709 ("The first offer can come on the 15 December 1948 turn,
/// not earlier") and Q-OBJ-4 ("On a front's armistice, that front's units leave the board"). Gdd03MovementFortTests' board
/// and the real catalogue (military.armistice_quiet_turns 2); the fixture's combat table agrees on both faces.</summary>
public class Gdd03ArmisticeQuietTests
{
    // A hard armistice card with the real cards' two options (content/events/armistice_jordan.yaml). It is dated long after
    // the turns these tests play, so it is pending only where a test puts it there: left unanswered, it would resolve by itself.
    const string JordanCard = @"
id: armistice_jordan
title: Armistice with Jordan
text: '{prime_minister} must decide whether Israel signs.'
office: prime_minister
anchor: hard
from: 1950-01-01
options:
  - id: sign
    text: Sign the armistice
    historical: true
  - id: hold_out
    text: Refuse and keep fighting
    alternative: { basis: design }
";

    static Content Load(string ruleset = SaveGame.Gdd03Ruleset)
    {
        var f = Gdd03MovementFortTests.Files(ruleset);
        f["events/armistice_jordan.yaml"] = JordanCard;
        return ContentLoader.Load(Fixture.Write(f));
    }

    // The enemy is weaker than the player's 20 everywhere, so the enemy script holds and only the player's orders engage.
    static Unit Ours(int id, int q, int r, int strength = 6) => new Unit(id, "recruit", q, r, side: "israel", strength: strength);
    static Unit Theirs(int id, int q, int r, string side, int strength = 6) => new Unit(id, "enemy", q, r, side: side, strength: strength);

    static GameState With(Content c, params Unit[] units) => CombatTests.With(c, units);

    static GameState Turn(Content c, GameState s, params Command[] orders)
    {
        var r = Simulation.Step(c, s, orders);
        Assert.Empty(r.Rejected);
        return r.State;
    }

    static int[] Counts(GameState s) => Prisoners.Fronts.Select(f => Armistice.QuietTurns(s, f)).ToArray();   // egypt, jordan, lebanon, syria

    // R-WAR-005, Q48: the count runs per front from the scenario start (turns long before 16 November 1948 included): a turn
    // with no engagement adds 1 to each of the four fronts.
    [Fact]
    public void AQuietTurnCountsForEveryFrontWithNoEngagement()
    {
        var c = Load();
        var s = With(c, Ours(1, 0, 1), Theirs(2, 5, 1, "egypt"));
        Assert.Equal(new[] { 0, 0, 0, 0 }, Counts(s));
        s = Turn(c, s);
        Assert.Equal(new[] { 1, 1, 1, 1 }, Counts(s));
        s = Turn(c, s);
        Assert.Equal(new[] { 2, 2, 2, 2 }, Counts(s));
    }

    // R-WAR-005, Q48: an engagement with an Egyptian unit resets Egypt only; an Iraqi unit resets Jordan (the Jordan agreement
    // took over the Iraqi sector); an ALA or local unit belongs to no front and resets none.
    [Theory]
    [InlineData("egypt", new[] { 0, 2, 2, 2 })]
    [InlineData("iraq", new[] { 2, 0, 2, 2 })]
    [InlineData("ala", new[] { 2, 2, 2, 2 })]
    [InlineData("local", new[] { 2, 2, 2, 2 })]
    public void AnEngagementResetsOnlyItsOwnFront(string side, int[] after)
    {
        var c = Load();
        var s = Turn(c, With(c, Ours(1, 2, 1, 20), Theirs(2, 3, 1, side)));
        Assert.Equal(new[] { 1, 1, 1, 1 }, Counts(s));
        s = Turn(c, s, new UnitOrder(1, Combat.Attack, 3, 1));
        Assert.Equal(after, Counts(s));
    }

    // R-WAR-005, Q48: an engagement with an Egyptian and a Jordanian defender breaks the count of both fronts.
    [Fact]
    public void AMixedFrontEngagementResetsEveryFrontInIt()
    {
        var c = Load();
        var s = Turn(c, With(c, Ours(1, 2, 1, 20), Theirs(2, 3, 1, "egypt"), Theirs(3, 3, 1, "jordan")));
        s = Turn(c, s, new UnitOrder(1, Combat.Attack, 3, 1));
        Assert.Equal(new[] { 0, 0, 2, 2 }, Counts(s));
    }

    // R-WAR-005, Q48: a refused order breaks nothing (a truce's refusal, R-COMBAT-009, or an attack on an empty hex). An
    // engagement counts whoever attacked: the player's instant attack in planning and the enemy's attack in step 2 alike.
    [Fact]
    public void ARefusedOrderDoesNotBreakTheCount()
    {
        var c = Load();
        var s = Turn(c, With(c, Ours(1, 2, 1, 20), Theirs(2, 3, 1, "egypt")));
        s.ChosenOptions["first_truce"] = "accept";   // the first truce in force until 8 July 1948
        var refused = Simulation.Step(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 3, 1), new UnitOrder(1, Combat.Attack, 1, 1) });
        Assert.Equal(2, refused.Rejected.Count);
        Assert.Equal(new[] { 2, 2, 2, 2 }, Counts(refused.State));

        var t = With(c, Ours(1, 2, 1), Theirs(2, 3, 1, "syria"));
        var why = new List<Cause>();
        Assert.Null(Combat.Give(c, t, 2, 2, 1, why, enemy: true));
        Combat.Resolve(c, t, why);
        Assert.Contains(why, w => w.Key == WhyKeys.Engagement);
        Assert.Equal(new[] { "syria" }, t.EngagedFronts.ToArray());
    }

    // R-WAR-005 (owner, 27 October 2026; Q48): no early offer before the 15 December 1948 turn; there, two quiet turns open
    // one and one does not. A signed front gets no offer.
    [Fact]
    public void NoOfferBeforeTheFifteenthOfDecemberTurnAndTwoQuietTurnsOpenOne()
    {
        var c = Load();
        var s = With(c);
        s.QuietTurns["egypt"] = 2;
        s.Date = new DateTime(1948, 11, 15);
        Assert.False(Armistice.MayOfferEarly(c, s, "egypt"));
        s.Date = new DateTime(1948, 12, 15);
        Assert.True(Armistice.MayOfferEarly(c, s, "egypt"));
        s.QuietTurns["egypt"] = 1;
        Assert.False(Armistice.MayOfferEarly(c, s, "egypt"));
        s.QuietTurns["egypt"] = 2;
        s.ChosenOptions["armistice_egypt"] = "sign";
        Assert.False(Armistice.MayOfferEarly(c, s, "egypt"));
    }

    // R-WAR-006, Q49 ("Does the refusal turn count toward the next quiet period? No. Two full quiet turns must follow the
    // refusal.", owner, 1 November 2026; #1709 open question 4: the refusal is the hard card's hold_out): a refusal on turn T
    // ends T at 0 for its front, T+1 and T+2 quiet make it eligible on T+3; other fronts keep counting.
    [Fact]
    public void HoldOutResetsTheCountAndTwoNewQuietTurnsAreNeeded()
    {
        var c = Load();
        var s = With(c, Ours(1, 0, 1));
        s.Date = new DateTime(1948, 12, 15);
        s.QuietTurns["jordan"] = 5; s.QuietTurns["egypt"] = 5;
        Assert.True(Armistice.MayOfferEarly(c, s, "jordan"));
        s.PendingEvents.Add("armistice_jordan");
        s = Turn(c, s, new ChooseEventOption("armistice_jordan", "hold_out"));
        Assert.Equal((0, 6), (Armistice.QuietTurns(s, "jordan"), Armistice.QuietTurns(s, "egypt")));
        Assert.False(Armistice.MayOfferEarly(c, s, "jordan"));
        s = Turn(c, s);
        Assert.Equal(1, Armistice.QuietTurns(s, "jordan"));
        Assert.False(Armistice.MayOfferEarly(c, s, "jordan"));
        s = Turn(c, s);
        Assert.True(Armistice.MayOfferEarly(c, s, "jordan"));
    }

    // R-WAR-005, replay: saves are command logs, so the fronts engaged in the open turn are rebuilt by replaying its orders: an
    // open turn after an instant attack replays to the same state and hash, and End turn from the same log counts the same.
    [Fact]
    public void AnOpenTurnAfterAnInstantAttackReplaysToTheSameCount()
    {
        var c = Load();
        var s = Turn(c, With(c, Ours(1, 2, 1, 20), Theirs(2, 3, 1, "lebanon")));
        var log = new Command[] { new UnitOrder(1, Combat.Attack, 3, 1) };
        var open = Simulation.Preview(c, s, log).State;
        Assert.Equal(new[] { "lebanon" }, open.EngagedFronts.ToArray());
        Assert.Equal(open.Hash(), Simulation.Preview(c, s.Clone(), log).State.Hash());
        Assert.Equal(open.Hash(), open.Clone().Hash());
        var ended = Turn(c, s, log);
        Assert.Equal(new[] { 2, 2, 0, 2 }, Counts(ended));
        Assert.Empty(ended.EngagedFronts);
    }

    // R-WAR-005, R-SAVE-003, plan test 6: a save and reload between an instant attack and End turn, through a real schema-5
    // save of the real content, rebuilds the fronts engaged in the open turn and gives the same hash, and End turn from both
    // gives the same counts and hash (the pattern of NoShotArmsSessionTests).
    [Fact]
    public void ASaveAndLoadBetweenAnInstantAttackAndEndTurnGivesTheSameCountAndHash()
    {
        var root = RealContentTests.ContentRoot();
        var c = ContentLoader.Load(rel =>
        {
            var path = Path.Combine(root, rel);
            if (!File.Exists(path)) return null;
            var text = File.ReadAllText(path);
            return rel == "eras.yaml" ? text.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3") : text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));

        var game = GameSession.New(c, GoldenRun.Seed, Mode.Historical);
        var attack = (from u in game.Now.State.Units.Values.Where(u => u.Side == Movement.PlayerSide)
                      from e in game.Now.State.Units.Values.Where(e => Prisoners.FrontOf.ContainsKey(e.Side))
                      where Combat.Check(c, game.Now.State, u.Id, e.Q, e.R) == null
                      select new UnitOrder(u.Id, Combat.Attack, e.Q, e.R)).First();
        Assert.Null(game.Commit(attack));
        var engaged = game.Now.State.EngagedFronts.ToArray();
        Assert.NotEmpty(engaged);

        var text = game.Save.Write();
        Assert.StartsWith("statecraft-save 5", text);
        var replay = GameSession.Load(c, text, out _);
        Assert.Equal(game.Now.State.EngagedFronts.ToArray(), replay.Now.State.EngagedFronts.ToArray());
        Assert.Equal(game.Now.State.Hash(), replay.Now.State.Hash());

        var ended = game.EndTurn(Array.Empty<Command>());
        var replayEnd = replay.EndTurn(Array.Empty<Command>());
        Assert.Equal(Counts(ended.State), Counts(replayEnd.State));
        Assert.All(engaged, f => Assert.Equal(0, Armistice.QuietTurns(ended.State, f)));
        Assert.Equal(ended.State.Hash(), replayEnd.State.Hash());
    }

    // R-WAR-005 to R-WAR-007 are gdd-0.3 only: under gdd-0.1 no count is kept or hashed and signing removes nothing (#752
    // owns R-WAR-007 there). The pinned goldens are the GoldenRun tests' job and stay as they are.
    [Fact]
    public void Gdd01IsUnchanged()
    {
        var c = Load(SaveGame.GddRuleset);
        var s = With(c, Ours(1, 2, 1, 20), Theirs(2, 3, 1, "jordan"), Theirs(3, 5, 0, "jordan"));
        s.PendingEvents.Add("armistice_jordan");
        s = Turn(c, s, new UnitOrder(1, Combat.Attack, 3, 1), new ChooseEventOption("armistice_jordan", "sign"));
        Assert.Empty(s.QuietTurns);
        Assert.Empty(s.EngagedFronts);
        Assert.True(s.Units.ContainsKey(3));
        Assert.DoesNotContain("\nquiet{", s.Describe());
        Assert.DoesNotContain("\nengaged[", s.Describe());
    }

    // R-WAR-007, Q-OBJ-4: signing armistice_jordan removes the Jordanian and the Iraqi units, leaves the Egyptian, ALA and
    // local units and the prisoners held as they were. Send home after signing is covered by
    // SendHomeAfterSigningLooksAtTheFrontsStillOnTheBoard.
    [Fact]
    public void SigningRemovesThatFrontsUnitsOnly()
    {
        var c = Load();
        var s = With(c, Ours(1, 2, 1), Theirs(2, 3, 1, "jordan"), Theirs(3, 2, 2, "iraq"), Theirs(4, 5, 0, "egypt"),
            Theirs(5, 5, 2, "ala"), Theirs(6, 0, 0, "local"));
        s.PrisonersHeld["jordan"] = 3; s.SoldiersHeld["iraq"] = 1;
        s.PendingEvents.Add("armistice_jordan");
        var why = new List<Cause>();
        Assert.Null(EventSystem.Choose(c, s, "armistice_jordan", "sign", why));
        Assert.Equal(new[] { 1, 4, 5, 6 }, s.Units.Keys.ToArray());
        Assert.Equal((3, 1), (s.PrisonersHeld["jordan"], s.SoldiersHeld["iraq"]));
    }

    // R-WAR-007 with R-UNIT-005 unchanged: unit 1 borders Jordan's unit 2 and Egypt's unit 3, so its front is the lower id's,
    // Jordan. Once Jordan's units leave on signing, the unit's nearest front-bearing enemy is Egypt's, and Send home waits for
    // that armistice. Without the removal the front would still be Jordan, signed, and Send home would be open.
    [Fact]
    public void SendHomeAfterSigningLooksAtTheFrontsStillOnTheBoard()
    {
        var c = Load();
        var s = With(c, Ours(1, 2, 1), Theirs(2, 3, 1, "jordan"), Theirs(3, 1, 1, "egypt"));
        Assert.Equal("The armistice with Jordan is not signed", Demobilization.Check(c, s, 1));
        s.PendingEvents.Add("armistice_jordan");
        Assert.Null(EventSystem.Choose(c, s, "armistice_jordan", "sign", new List<Cause>()));
        Assert.Equal(new[] { 1, 3 }, s.Units.Keys.ToArray());
        Assert.Equal("The armistice with Egypt is not signed", Demobilization.Check(c, s, 1));
    }
}
