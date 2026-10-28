using Statecraft.Game.UI;

namespace Statecraft.Sim.Tests;

public class CameraHeadingPolicyTests
{
    // R-CAMERA-001
    [Theory]
    [InlineData("legacy", false)]
    [InlineData("gdd-0.1", false)]
    [InlineData("gdd-0.3", true)]
    [InlineData("gdd-0.30", false)]
    [InlineData("gdd-0.3 ", false)]
    [InlineData(" gdd-0.3", false)]
    [InlineData("GDD-0.3", false)]
    [InlineData("gdd-0.3-preview", false)]
    [InlineData("", false)]
    public void OnlyTheExactRulesetFixesNorth(string ruleset, bool expected) =>
        Assert.Equal(expected, CameraHeadingPolicy.IsFixedNorth(ruleset));
}
