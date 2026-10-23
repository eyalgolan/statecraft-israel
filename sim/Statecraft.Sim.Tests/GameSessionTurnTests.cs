using System;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>R-TURN-004 and R-SAVE-003 (spec 2026-10-23 sections 3.3 and 3.4): committed orders are final, shown at once,
/// run first at End turn, and survive a reload.</summary>
public class GameSessionTurnTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    static GameSession Fresh() => GameSession.New(C, GoldenRun.Seed, Mode.Historical);

    [Fact]
    public void ACommittedMoveShowsAtOnceAndEndTurnRunsIt()
    {
        var session = Fresh();
        var move = GoldenRun.SmokeMove(C, session.State);
        var before = (session.State.Units[move.Unit].Q, session.State.Units[move.Unit].R);
        Assert.Null(session.Commit(move));
        Assert.Equal((move.Q, move.R), (session.Now.State.Units[move.Unit].Q, session.Now.State.Units[move.Unit].R));
        Assert.Equal(before, (session.State.Units[move.Unit].Q, session.State.Units[move.Unit].R));
        var ended = session.EndTurn(new Command[0]);
        Assert.Equal(GoldenRun.PlaySmokeOrders(C).Hash(), ended.State.Hash());
        Assert.Empty(session.Committed);
    }

    [Fact]
    public void ARefusedOrderIsNotCommitted()
    {
        var session = Fresh();
        Assert.Equal("That unit is already there", session.Commit(new Move(0, session.State.Units[0].Q, session.State.Units[0].R)));
        Assert.Empty(session.Committed);
        Assert.Throws<ArgumentException>(() => session.Commit(new BuyArms(1)));
        Assert.Throws<ArgumentException>(() => session.Commit(new UnitOrder(0, Demobilization.SendHome)));   // waits for End turn
    }

    // R-TURN-004: deferred orders run after the committed ones, whatever order they were given in.
    [Fact]
    public void DeferredOrdersRunAfterCommittedOnes()
    {
        var session = Fresh();
        var move = GoldenRun.SmokeMove(C, session.State);
        session.Commit(move);
        session.EndTurn(new Command[] { new BuyArms(1) });
        Assert.Equal(new Command[] { move }.Concat(new Command[] { new BuyArms(1) }).Select(x => x.Kind),
                     session.Save.Turns.Last().Select(x => x.Kind));
    }

    // R-SAVE-003: a reload in the middle of a turn keeps what was done.
    [Fact]
    public void AReloadKeepsTheOpenTurn()
    {
        var session = Fresh();
        var move = GoldenRun.SmokeMove(C, session.State);
        session.Commit(move);
        var loaded = GameSession.Load(C, session.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(session.Now.State.Hash(), loaded.Now.State.Hash());
        Assert.Equal(new[] { "move" }, loaded.Committed.Select(x => x.Kind));
    }

    [Fact]
    public void StepRefusesWhileOrdersAreCommitted()
    {
        var session = Fresh();
        session.Commit(GoldenRun.SmokeMove(C, session.State));
        Assert.Throws<InvalidOperationException>(() => session.Step(new Command[0]));
    }

    // R-SAVE-004: a schema 2 GDD save with a player attack (a standing order then) is refused as older rules.
    [Fact]
    public void ASchema2SaveWithAnAttackIsRefusedAsOlderRules()
    {
        var session = Fresh();
        var text = session.Save.Write().Replace("statecraft-save 3", "statecraft-save 2")
            .Replace("end ", "turn\norder 0 attack 10 10\nend ");
        var e = Assert.Throws<SaveException>(() => GameSession.Load(C, text, out _));
        Assert.Equal(SaveException.OlderRules, e.Code);
    }

    // R-SAVE-003: reloading keeps an attack's result; the same draw comes out.
    [Fact]
    public void AReloadKeepsAnAttacksResult()
    {
        var session = Fresh();
        var (unit, q, r) = FirstAttack(session);
        Assert.Null(session.Commit(new UnitOrder(unit, Combat.Attack, q, r)));
        var loaded = GameSession.Load(C, session.Save.Write(), out _);
        Assert.Equal(session.Now.State.Hash(), loaded.Now.State.Hash());
    }

    // R-TURN-004: a vote queued after an attack runs after it, so it cannot change the attack's draw.
    [Fact]
    public void AVoteQueuedAfterAnAttackDoesNotChangeIt()
    {
        var a = Fresh(); var b = Fresh();
        var (unit, q, r) = FirstAttack(a);
        a.Commit(new UnitOrder(unit, Combat.Attack, q, r)); b.Commit(new UnitOrder(unit, Combat.Attack, q, r));
        var policy = C.At(a.State.Date).Policies.First().Id;
        var withVote = a.EndTurn(new Command[] { new SetPolicy(policy, 0.5) });
        var without = b.EndTurn(new Command[0]);
        Assert.Equal(without.Why.First(w => w.Message.StartsWith("The attack on hex")).Message,
                     withVote.Why.First(w => w.Message.StartsWith("The attack on hex")).Message);
    }

    /// <summary>The first (unit, hex) of the player's that Combat.Check takes on the session's shown state. At the start Golani
    /// (unit 0, 10,9) is next to the Syrian unit on 10,10 (docs/determinism.md, #1240's row), so one exists.</summary>
    static (int, int, int) FirstAttack(GameSession s)
    {
        var st = s.Now.State;
        foreach (var u in st.Units.Values.Where(x => x.Side == Movement.PlayerSide))
            foreach (var e in st.Units.Values.Where(x => x.Side != "" && x.Side != Movement.PlayerSide))
                if (Combat.Check(C, st, u.Id, e.Q, e.R, spent: s.Now.Spent) == null) return (u.Id, e.Q, e.R);
        throw new InvalidOperationException("precondition: no attack is possible at the start");
    }

    // R-SAVE-004: a schema 2 GDD save that does not replay to its saved hash gets the clear older-rules message; a schema 3
    // one that does not is still damaged (no code).
    [Fact]
    public void ASchema2SaveThatReplaysDifferentlyIsRefusedAsOlderRules()
    {
        static string WrongEnd(string text) { var end = text.Substring(text.LastIndexOf("end ", StringComparison.Ordinal) + 4, 64); return text.Replace("end " + end, "end " + new string('0', 64)); }
        var two = WrongEnd(Fresh().Save.Write().Replace("statecraft-save 3", "statecraft-save 2"));
        Assert.Equal(SaveException.OlderRules, Assert.Throws<SaveException>(() => GameSession.Load(C, two, out _)).Code);
        var three = WrongEnd(Fresh().Save.Write());
        Assert.Null(Assert.Throws<SaveException>(() => GameSession.Load(C, three, out _)).Code);
    }
}
