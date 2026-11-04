using System;
using Statecraft.Sim;

public class CloneImmutabilityTests
{
    // #2513: Qualified.Halutzim and CorridorWork.Segments used to store raw arrays behind IReadOnlyList, and
    // GameState.Clone() shares the records, so a cast back to the array could change both states. Both are
    // Array.AsReadOnly copies now: the cast that would mutate the shared store throws, so a clone cannot reach it
    // and the original's hash is untouched.
    [Fact]
    public void CloneSharesNoMutableArrayBehindTheReadOnlyList()
    {
        var s = new GameState { Date = new DateTime(1948, 6, 15), RngState = 3, Mode = Mode.Historical };
        s.Cities["town_a"] = new City("town_a", 10, 5, 20).WithQualified(new Qualified(0, 0, new[] { 1, 2 }));
        s.Corridors["c1"] = new CorridorWork("c1", new SegmentWork?[] { null });

        var clone = s.Clone();
        Assert.Throws<InvalidCastException>(() => (int[])clone.Cities["town_a"].Qualified.Halutzim);
        Assert.Throws<InvalidCastException>(() => (SegmentWork?[])clone.Corridors["c1"].Segments);
        Assert.Equal(s.Hash(), clone.Hash());
    }
}
