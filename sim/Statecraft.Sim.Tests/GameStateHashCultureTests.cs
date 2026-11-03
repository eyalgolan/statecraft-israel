using System;
using System.Globalization;
using Statecraft.Sim;

/// <summary>The state hash and its readable text must not depend on the machine's culture: he-IL writes a negative
/// number with a left-to-right mark before the minus sign (U+200E U+002D), en-US with the minus sign alone.</summary>
public class GameStateHashCultureTests
{
    /// <summary>A legacy state whose every integer written by GameState's own text builder is negative, including a
    /// map placement on a negative hex.</summary>
    static GameState NegativeState()
    {
        var s = new GameState { Date = new DateTime(1948, 6, 1), RngState = 1, Turn = -3, Rung = -2, CeasefireWeeksLeft = -4, MapActionsThisTurn = -1 };
        s.Placements.Add(new Placement(MapActions.TransitCamp, -5, -7, s.Date));
        return s;
    }

    static (string Hash, string Text) Under(CultureInfo culture)
    {
        var prev = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            var s = NegativeState();
            return (s.Hash(), s.Describe());
        }
        finally { CultureInfo.CurrentCulture = prev; }
    }

    /// <summary>he-IL with its negative sign set explicitly, so the test does not rely on this machine's ICU data.</summary>
    static CultureInfo Hebrew()
    {
        var he = (CultureInfo)new CultureInfo("he-IL").Clone();
        he.NumberFormat.NegativeSign = "‎-";
        return he;
    }

    [Fact]
    public void HashAndTextAreTheSameUnderHebrewAndEnglish()
    {
        var invariant = Under(CultureInfo.InvariantCulture);
        Assert.Contains("@-5,-7@", invariant.Text);
        Assert.Equal(invariant, Under(new CultureInfo("en-US")));
        Assert.Equal(invariant, Under(Hebrew()));
    }
}
