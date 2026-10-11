using System;

namespace Statecraft.Sim
{
    public enum TurnLength { Quarter, Month, Week }

    public static class SimTime
    {
        public static DateTime Advance(DateTime date, TurnLength len) => len switch
        {
            TurnLength.Quarter => date.AddMonths(3),
            TurnLength.Month => date.AddMonths(1),
            _ => date.AddDays(7),
        };

        /// <summary>The end of the turn that starts on <paramref name="date"/> when the era ends on <paramref name="boundary"/>
        /// (spec 4.1). A turn that would reach or pass the boundary ends on it. A turn that would leave under 14 days
        /// before it is lengthened to end on it, so the player never gets a turn of a few days with a full turn of
        /// attention. Any other turn is the normal one.</summary>
        public static DateTime AdvanceTo(DateTime date, TurnLength len, DateTime boundary)
        {
            var next = Advance(date, len);
            return next >= boundary || (boundary - next).TotalDays < 14 ? boundary : next;
        }

        /// <summary>Share of a year one turn represents (used to scale yearly rates).</summary>
        public static double YearFraction(TurnLength len) => len switch
        {
            TurnLength.Quarter => 0.25,
            TurnLength.Month => 1.0 / 12,
            _ => 7.0 / 365,
        };
    }
}
