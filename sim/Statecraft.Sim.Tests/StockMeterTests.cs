using System.Globalization;
using Statecraft.Game.UI;

public class StockMeterTests
{
    static string Format(string key, long n) => n.ToString(key == "hud.delta_stock" ? "+#,##0;-#,##0" : "N0", CultureInfo.InvariantCulture);

    [Fact]
    public void OpeningStockAndUnchangedStockHaveNoChange()
    {
        foreach (var before in new int?[] { null, 1200 })
        {
            var m = StockMeter.From(1200, before, Format);
            Assert.Equal("1,200", m.Value);
            Assert.Equal("1,200", m.SayValue);
            Assert.Equal("", m.Delta);
            Assert.Equal("", m.SayChange);
            Assert.Equal(0, m.Sign);
        }
    }

    [Theory]
    [InlineData(12, 10, "+2", 1, "2")]
    [InlineData(0, 12, "-12", -1, "12")]
    [InlineData(int.MaxValue, 0, "+2,147,483,647", 1, "2,147,483,647")]
    public void ChangesAreWholeStockUnits(int now, int before, string delta, int sign, string said)
    {
        var m = StockMeter.From(now, before, Format);
        Assert.Equal(delta, m.Delta);
        Assert.Equal(sign, m.Sign);
        Assert.Equal(said, m.SayChange);
    }
}
