using System;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>#2733 item 10, R-SAVE-003: the open-turn split on load when an order the save holds is refused after one the
/// sim takes. The tests in GameSessionTurnTests (#2653) put the refused order first, so the accepted prefix was empty.</summary>
public class SaveOpenTurnSplitTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    static GameSession Fresh() => GameSession.New(C, GoldenRun.Seed, Mode.Historical);

    // An accepted move, then a refused one: a player unit other than the moved one, sent where it already stands.
    static (Move Valid, Move Refused) Orders(GameState s)
    {
        var valid = GoldenRun.SmokeMove(C, s);
        var other = s.Units.First(kv => kv.Key != valid.Unit && kv.Value.Side == Movement.PlayerSide);
        return (valid, new Move(other.Key, other.Value.Q, other.Value.R));
    }

    // Same content: a refused open-turn order is damage, even after an order the sim takes.
    [Fact]
    public void WithTheSameContentARefusalAfterAnAcceptedOrderIsAnError()
    {
        var session = Fresh();
        var (valid, refused) = Orders(session.State);
        session.Save.SetOpen(new Command[] { valid, refused });
        var e = Assert.Throws<SaveException>(() => GameSession.Load(C, session.Save.Write(), out _));
        Assert.Contains("The open turn's order 'move' was refused", e.Message);
    }

    // Other content: the orders before the refused one load as the open turn, and the load warns that the replay differs.
    [Fact]
    public void WithOtherContentTheAcceptedPrefixLoadsWithReplayDiffers()
    {
        var session = Fresh();
        var (valid, refused) = Orders(session.State);
        Assert.Equal("That unit is already there", Fresh().Commit(refused));
        session.Save.SetOpen(new Command[] { valid, refused });
        var text = session.Save.Write().Replace("content " + C.Hash, "content " + new string('0', 64));   // other content
        var loaded = GameSession.Load(C, text, out var warnings);
        Assert.Equal(new[] { GameSession.ContentChanged, GameSession.ReplayDiffers }, warnings);
        Assert.Equal(new[] { valid.Unit }, loaded.Committed.Cast<Move>().Select(m => m.Unit));
        var expected = Fresh();
        Assert.Null(expected.Commit(valid));
        Assert.Equal(expected.Now.State.Hash(), loaded.Now.State.Hash());
    }
}
