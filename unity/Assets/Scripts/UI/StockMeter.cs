#nullable enable
using System;

namespace Statecraft.Game.UI
{
    /// <summary>One stock reading: its value and the change since the last turn (empty when there is none), and the
    /// two again in words: SayValue, and SayChange without its sign (empty when there is no change).</summary>
    public readonly struct StockReading
    {
        public readonly string Value, Delta, SayValue, SayChange;
        /// <summary>+1 up, -1 down, 0 unchanged or unknown.</summary>
        public readonly int Sign;
        public StockReading(string value, string delta, int sign, string sayValue, string sayChange)
        { Value = value; Delta = delta; Sign = sign; SayValue = sayValue; SayChange = sayChange; }
    }

    /// <summary>Whole stock units, with no indicator or population conversion. Shared by Food and Arms.</summary>
    public static class StockMeter
    {
        public static StockReading From(int now, int? before, Func<string, long, string> format)
        {
            var value = format("hud.stock", now);
            long change = before.HasValue ? (long)now - before.Value : 0;
            return change == 0 ? new StockReading(value, "", 0, value, "")
                : new StockReading(value, format("hud.delta_stock", change), Math.Sign(change), value, format("hud.stock", Math.Abs(change)));
        }
    }
}
