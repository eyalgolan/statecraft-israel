using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Statecraft.Sim;

public class SettlementStateTests
{
    // R-CTRL-002
    [Fact]
    public void AnEmptyStateHashesAsBefore()
    {
        var state = new GameState();
        var before = state.Describe();
        state.Settlements = new SortedDictionary<string, SettlementControl>(StringComparer.Ordinal);
        Assert.Equal(before, state.Describe());
        Assert.DoesNotContain("ctrl", state.Describe());
    }

    // R-CTRL-002
    [Fact]
    public void ASettlementStateHashesInOrdinalOrderAndOmitsDefaultFields()
    {
        var state = new GameState();
        state.Settlements["z_place"] = new SettlementControl(state: "occupied", combatants: 2);
        state.Settlements["m_place"] = new SettlementControl(depopulated: true);
        state.Settlements["a_place"] = new SettlementControl(owner: "player");
        var text = state.Describe();
        Assert.Contains("ctrl{\n  a_place=owner=player\n  m_place=depopulated=1\n  z_place=state=occupied,combatants=2", text);
        Assert.DoesNotContain("since=", text);
        Assert.DoesNotContain("buildings=", text);
        Assert.True(text.IndexOf("a_place", StringComparison.Ordinal) < text.IndexOf("z_place", StringComparison.Ordinal));
    }

    // R-CTRL-002
    [Fact]
    public void CloneCopiesTheCollectionButSharesItsImmutableRecords()
    {
        var state = new GameState();
        state.Settlements["place"] = new SettlementControl(depopulated: true);
        var clone = state.Clone();
        Assert.NotSame(state.Settlements, clone.Settlements);
        Assert.Same(state.Settlements["place"], clone.Settlements["place"]);
    }

    // R-CTRL-003
    [Fact]
    public void ResidentsAreNotCombatants()
    {
        var properties = typeof(SettlementControl).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        // Q-CAP-2 (b): the residents are one yes/no state, not a count; the only count on the record is the combatants.
        Assert.Equal(typeof(bool), properties.Single(x => x.Name == nameof(SettlementControl.Depopulated)).PropertyType);
        Assert.Equal(new[] { nameof(SettlementControl.Combatants) },
            properties.Where(x => x.PropertyType == typeof(int) || x.PropertyType == typeof(long) || x.PropertyType == typeof(double)).Select(x => x.Name).ToArray());
        Assert.DoesNotContain(properties, x => x.Name.Contains("Resident", StringComparison.Ordinal) || x.Name.Contains("Population", StringComparison.Ordinal));
    }
}
