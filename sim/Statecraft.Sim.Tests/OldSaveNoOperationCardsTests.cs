using System.IO;
using Statecraft.Sim;

/// <summary>
/// The fixture is a real gdd-0.1 save written by v0.3.4, from a game in which no operation card ever fires, so the
/// operation cards' unit orders (#2087) cannot change its replay. It runs to 15 September 1948, past the Syrian army's
/// 15 July 1948 step onto its bridgehead anchor (#1270, S1), so it replays differently by design (owner, 4 Nov 2026, on
/// #2180) and loads with replay_differs. Any other replay change still needs an owner decision, not a fixture to
/// regenerate.
/// </summary>
public class OldSaveNoOperationCardsTests
{
    // GameState.Hash() of this save's replay after S1 (the Syrian army's 15 July 1948 step, #1270), reviewed and accepted by
    // the owner's 4 Nov 2026 ruling "(a) Accept replay differs (Recommended)" on #2180. Re-pinned for #2804 item 4
    // (TotalPopulation summed in ordinal key order, a rounding-only move; owner on #2804, 6 Nov 2026: "Do it just before
    // the regen (Recommended)"; was bec1464e).
    private const string AcceptedReplayHashAfterS1 = "ca30ddf09dfc46eefef11c5528b864fa664aaa77f129786d79867b17fba4dc9c";

    [Fact]
    public void V034Gdd01SaveWithNoOperationCardLoadsAndReportsReplayDiffers()
    {
        var content = ContentLoader.Load(RealContentTests.ContentRoot());
        var repo = Directory.GetParent(RealContentTests.ContentRoot())!.FullName;
        var path = Path.Combine(repo, "sim", "Statecraft.Sim.Tests", "saves", "v0.3.4-gdd01-noops.txt");
        var text = File.ReadAllText(path);

        var saved = SaveGame.Parse(text);
        Assert.Equal(SaveGame.GddRuleset, saved.Ruleset);

        // The operation cards: the events whose options give unit orders under gdd-0.1 (#2087). A card given unit
        // orders later fails here, so this fixture is checked against it.
        var operationCards = content.Events.Where(e => e.Options.Any(o => o.UnitOrders.Count > 0)).Select(e => e.Id)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(operationCards);
        Assert.Equal(
            new[] { "latrun_proposal", "operation_hiram", "operation_horev", "operation_yoav", "ten_days" }, operationCards);

        var loaded = GameSession.Load(content, text, out var warnings);

        // The game is four turns, 15 May to 15 September 1948: first_truce is refused, so ten_days (which requires
        // first_truce accept) never fires; second_truce is defied, so latrun_proposal (which requires second_truce comply)
        // never fires; operation_yoav, operation_hiram and operation_horev open on 15 October, 31 October and 15 December
        // 1948, after the last turn ends. So no operation card is chosen or delegated. If one fires here, a content change
        // moved it into this game and the fixture no longer keeps clear of the operation cards' rules.
        Assert.DoesNotContain(loaded.State.FiredEvents, operationCards.Contains);

        // The replay and the open turn first: with other content, drift only adds replay_differs, so checking the
        // warnings first would hide which part drifted.
        // If this fails after a deliberate gdd-0.1 rule or design-value change (a golden re-pin, docs/determinism.md),
        // v0.3.4 players' saves no longer replay identically: they load with replay_differs (GameSession.Load). That needs an
        // owner decision, not a fixture update or deleting this test. The operation cards' unit orders (#2087) were such a
        // decision for saves that reach those cards; this save never does, so it must still replay identically.
        // Then changed (4 Nov 2026, #2180, #1270; the lines above are the maintainers', unchanged): the Syrian script's S1
        // anchor (from 8 July 1948) steps the Syrian army onto it on the 15 July 1948 turn, inside this game. Asked whether
        // v0.3.4 gdd-0.1 saves reaching that day may replay differently, the owner chose "(a) Accept replay differs
        // (Recommended)"; the chosen option read: "as for #2087's cards on 3 November, old saves load with the content and
        // replay-changed notice, and the test is updated". So this save still loads, keeps its open order and warns
        // content_changed plus replay_differs, nothing else.
        // The accepted replay is pinned exactly: any further drift changes this hash and needs its own owner decision.
        // Re-pin only for a reviewed change, with the ruling that accepts it.
        Assert.Equal(AcceptedReplayHashAfterS1, loaded.State.Hash());
        var order = Assert.IsType<UnitOrder>(Assert.Single(loaded.Committed));
        Assert.Equal((3, Orders.Fortify), (order.Unit, order.Order));

        Assert.Equal(new[] { GameSession.ContentChanged, GameSession.ReplayDiffers }, warnings);
    }
}
