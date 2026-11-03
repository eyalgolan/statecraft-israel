using System.IO;
using Statecraft.Sim;

/// <summary>
/// The fixture is a real gdd-0.1 save written by v0.3.4. Replay drift under gdd-0.1 means a v0.3.4 player's save
/// no longer loads identically; that is a release blocker, not a fixture to regenerate.
/// </summary>
public class OldSaveCompatibilityTests
{
    [Fact]
    public void V034Gdd01SaveStillLoadsIdentically()
    {
        var content = ContentLoader.Load(RealContentTests.ContentRoot());
        var repo = Directory.GetParent(RealContentTests.ContentRoot())!.FullName;
        var path = Path.Combine(repo, "sim", "Statecraft.Sim.Tests", "saves", "v0.3.4-gdd01.txt");
        var text = File.ReadAllText(path);

        var saved = SaveGame.Parse(text);
        Assert.Equal(SaveGame.GddRuleset, saved.Ruleset);

        var loaded = GameSession.Load(content, text, out var warnings);

        // The replay and the open turn first: with other content, drift only adds replay_differs, so checking the
        // warnings first would hide which part drifted.
        Assert.Equal(saved.FinalHash, loaded.State.Hash());
        var order = Assert.IsType<UnitOrder>(Assert.Single(loaded.Committed));
        Assert.Equal((3, Orders.Fortify), (order.Unit, order.Order));

        Assert.Equal(new[] { GameSession.ContentChanged }, warnings);
    }
}
