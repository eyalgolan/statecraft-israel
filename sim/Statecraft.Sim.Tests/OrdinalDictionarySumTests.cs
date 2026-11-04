using System;
using System.Collections.Generic;
using System.Globalization;
using Statecraft.Sim;

public class OrdinalDictionarySumTests
{
    // #2511: two floating-point sums over a Dictionary depended on its insertion order (floating-point addition is not
    // associative, and Dictionary's order is an implementation detail). Both now sum in ordinal key order. Each test
    // builds the dictionary in two insertion orders whose naive sums differ (0.6 against 0.6000000000000001) and asserts
    // the identical result, no tolerance.

    const string ModelA = @"
indicators:
  - { id: morale, name: Morale }
  - { id: housing, name: Housing }
  - { id: security, name: Security }
voter_groups:
  - { id: everyone, weights: { security: 0.3, housing: 0.1, morale: 0.2 } }
";
    const string ModelB = @"
indicators:
  - { id: morale, name: Morale }
  - { id: housing, name: Housing }
  - { id: security, name: Security }
voter_groups:
  - { id: everyone, weights: { housing: 0.1, morale: 0.2, security: 0.3 } }
";

    static GameState Populated(string[] insertion)
    {
        var s = new GameState { Date = new DateTime(1948, 6, 15), RngState = 3, Mode = Mode.Historical };
        var values = new Dictionary<string, double> { ["a"] = 0.1, ["b"] = 0.2, ["c"] = 0.3 };
        foreach (var key in insertion) s.Population[key] = values[key];
        return s;
    }

    [Fact]
    public void TotalPopulationIsInsertionOrderIndependent()
    {
        var a = Populated(new[] { "b", "c", "a" });  // naive sum 0.6
        var b = Populated(new[] { "a", "c", "b" });  // naive sum 0.6000000000000001
        Assert.Equal(a.TotalPopulation, b.TotalPopulation);
    }

    [Fact]
    public void ApprovalWeightsSumIsInsertionOrderIndependent()
    {
        var a = EffectsSystem.Approval(ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["model.yaml"] = ModelA })),
            new GameState { Date = new DateTime(1948, 6, 15), Indicators = { ["morale"] = 1.0, ["housing"] = 1.0, ["security"] = 1.0 } });
        var b = EffectsSystem.Approval(ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["model.yaml"] = ModelB })),
            new GameState { Date = new DateTime(1948, 6, 15), Indicators = { ["morale"] = 1.0, ["housing"] = 1.0, ["security"] = 1.0 } });
        Assert.Equal(a, b);
    }
}
