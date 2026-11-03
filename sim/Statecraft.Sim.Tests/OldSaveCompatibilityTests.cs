using System.IO;
using Statecraft.Sim;

/// <summary>
/// The fixture is a real gdd-0.1 save written by v0.3.4. It delegates ten_days and chooses operation_yoav, whose unit
/// orders now apply under gdd-0.1 (R-ENEMY-005), so it replays differently by design (owner, 3 Nov 2026, #2087). Any
/// other replay change still needs an owner decision, not a fixture to regenerate.
/// </summary>
public class OldSaveCompatibilityTests
{
    [Fact]
    public void V034Gdd01SaveStillLoadsAndReportsReplayDiffers()
    {
        var content = ContentLoader.Load(RealContentTests.ContentRoot());
        var repo = Directory.GetParent(RealContentTests.ContentRoot())!.FullName;
        var path = Path.Combine(repo, "sim", "Statecraft.Sim.Tests", "saves", "v0.3.4-gdd01.txt");
        var text = File.ReadAllText(path);

        var saved = SaveGame.Parse(text);
        Assert.Equal(SaveGame.GddRuleset, saved.Ruleset);

        var loaded = GameSession.Load(content, text, out var warnings);

        // Owner, 3 Nov 2026, #2087, option (a) "Accept replay differs": a v0.3.4 save that chose or delegated an
        // operation card replays differently now that the cards' unit orders apply under gdd-0.1, and loads with
        // replay_differs. This save chose or delegated such a card, so it checks the accepted outcome: it still loads,
        // keeps its open order and warns content_changed plus replay_differs, nothing else. A v0.3.4 save that never
        // touches these cards still replays identically (OldSaveNoOperationCardsTests).
        var order = Assert.IsType<UnitOrder>(Assert.Single(loaded.Committed));
        Assert.Equal((3, Orders.Fortify), (order.Unit, order.Order));

        Assert.Equal(new[] { GameSession.ContentChanged, GameSession.ReplayDiffers }, warnings);
    }
}
