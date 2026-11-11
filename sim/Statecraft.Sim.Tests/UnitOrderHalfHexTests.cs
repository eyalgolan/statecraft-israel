using System;
using Statecraft.Sim;

/// <summary>#2526: a save writes a unit order's hex only when both coordinates are set, while live play refuses a
/// non-hex order that carries either one, so an order with half a hex would replay differently from the game that
/// ran it. The order now names both coordinates or neither (R-ORDER-001). First fixed by LetMeSleep8h in #2562.</summary>
public class UnitOrderHalfHexTests
{
    [Fact]
    public void AHalfGivenHexIsRefusedAtConstruction()
    {
        Assert.Throws<ArgumentException>(() => new UnitOrder(1, Orders.Hold, q: 1));
        Assert.Throws<ArgumentException>(() => new UnitOrder(1, Orders.Hold, r: 2));
        Assert.Throws<ArgumentException>(() => new UnitOrder(1, Movement.GoTo, q: 1));
        Assert.Throws<ArgumentException>(() => new UnitOrder(1, Combat.Attack, r: 0));
        // Both or neither still builds.
        Assert.Null(new UnitOrder(1, Orders.Hold).Q);
        Assert.Equal(2, new UnitOrder(1, Movement.GoTo, 1, 2).R);
    }
}
