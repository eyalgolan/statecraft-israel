#nullable enable
using System;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>One top-bar indicator: its value and the change since the last turn (empty when there is none).</summary>
    public readonly struct Meter
    {
        public readonly string Value, Delta;
        /// <summary>+1 up, -1 down, 0 unchanged or unknown.</summary>
        public readonly int Sign;
        public Meter(string value, string delta, int sign) { Value = value; Delta = delta; Sign = sign; }
    }

    /// <summary>What the top bar shows, computed from simulation state only (no UI types), so it is testable on its own.
    /// Indicators (0..1) show as 0-100; Treasury is the net position (cash minus debt, IL millions); Immigration is the
    /// number of immigrants who arrived during the last turn.</summary>
    public sealed class HudModel
    {
        public string Year = "", Turn = "", Date = "", Attention = "", SecurityLevel = "";
        public Meter Treasury, Immigration, Security, Support, Standing, Fx;

        static double Arrivals(StepResult? r) => r == null ? 0 : r.Why.Where(w => w.System == "population").Sum(w => w.Delta);

        public static HudModel From(SimBridge b, UiText t)
        {
            var c = b.Content; var s = b.State; var prev = b.Session.Previous;
            // Each meter: its value and, after the first turn, the change rounded as shown (a change that rounds to zero is
            // not shown). Money comes with its currency (PlayerText.Money: IL or $ from content); the 0..1 values are on the
            // 0-100 scale with "%" (PlayerText.Scale), as everywhere the player sees them (fix round 1, I2).
            Meter Money(string id, double now, double? before)
            {
                var value = PlayerText.Money(c, id, now);
                if (before == null) return new Meter(value, "", 0);
                var d = Math.Round(now - before.Value, 1, MidpointRounding.AwayFromZero);
                return d == 0 ? new Meter(value, "", 0) : new Meter(value, PlayerText.MoneyChange(c, id, d), Math.Sign(d));
            }
            Meter Points(double now, double? before)
            {
                var value = PlayerText.Percent(now);                   // the one formatter (fix round 2, N4)
                if (before == null) return new Meter(value, "", 0);
                // The difference of the two shown values, so the change always agrees with them (fix round 3, m-1).
                var points = PlayerText.ShownPoints(now) - PlayerText.ShownPoints(before.Value);
                return points == 0 ? new Meter(value, "", 0) : new Meter(value, PlayerText.PercentChangeShown(before.Value, now), Math.Sign(points));
            }
            Meter People(double now, double? before)
            {
                if (before == null) return new Meter(t.Format("hud.people", now), "", 0);
                var d = Math.Round(now - before.Value, 0, MidpointRounding.AwayFromZero);
                return d == 0 ? new Meter(t.Format("hud.people", now), "", 0) : new Meter(t.Format("hud.people", now), t.Format("hud.delta_people", d), Math.Sign(d));
            }
            return new HudModel
            {
                Year = s.Date.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Turn = t.Format("hud.turn", s.Turn),
                Date = t.Format("hud.date", PlayerText.Date(s.Date)),
                Attention = t.Format("hud.attention", b.AttentionLeft, c.RulesAt(s.Date).AttentionPerTurn),
                SecurityLevel = t.Format("hud.level", s.Rung),
                Treasury = Money("treasury", s.Treasury - s.Debt, prev == null ? (double?)null : prev.Treasury - prev.Debt),
                Immigration = People(Arrivals(b.Session.Last), b.BeforeLast == null ? (double?)null : Arrivals(b.BeforeLast)),
                Security = Points(s.Indicators["security"], prev?.Indicators["security"]),
                Support = Points(EffectsSystem.Approval(c, s), prev == null ? (double?)null : EffectsSystem.Approval(c, prev)),
                Standing = Points(s.Indicators["intl_standing"], prev?.Indicators["intl_standing"]),
                Fx = Money("fx_reserves", s.FxReserves, prev?.FxReserves),
            };
        }
    }
}
