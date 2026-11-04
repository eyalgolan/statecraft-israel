using System;
using Statecraft.Sim;

public class UnitOrderHalfHexTests
{
    // #2526: a save wrote a UnitOrder's coordinates only when both were set, while live play refused an order with
    // either one — so a half-given hex saved by no game could still have replayed differently. The constructor now
    // refuses both-or-neither, closing the case at the source: the command a save would have to carry cannot be built.
    [Fact]
    public void AHalfGivenHexIsRefusedAtConstruction()
    {
        Assert.Throws<ArgumentException>(() => new UnitOrder(1, "hold", q: 1));
        Assert.Throws<ArgumentException>(() => new UnitOrder(1, "hold", r: 2));
        Assert.Throws<ArgumentException>(() => new UnitOrder(1, Movement.GoTo, q: 1, r: null));
        // Both or neither remain fine.
        Assert.NotNull(new UnitOrder(1, "hold"));
        Assert.NotNull(new UnitOrder(1, Movement.GoTo, 1, 2));
    }
}
