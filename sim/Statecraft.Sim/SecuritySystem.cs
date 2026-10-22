using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>The always-on escalation ladder: 1 routine, 2 wave, 3 limited operation, 4 prolonged conflict, 5 full war.</summary>
    public static class SecuritySystem
    {
        /// <summary>The default `turn_lengths` table (rules without the key): quarter on rungs 1-3, month on 4, week on 5. The
        /// game reads <see cref="TurnLengthFor(Content, DateTime, int)"/>; this stays for the Defence screen's legend until the
        /// GDD cutover (spec 2026-10-12 section 3).</summary>
        public static TurnLength TurnLengthFor(int rung) =>
            rung >= 5 ? TurnLength.Week : rung == 4 ? TurnLength.Month : TurnLength.Quarter;

        /// <summary>The length of a turn that starts on <paramref name="date"/> on <paramref name="rung"/>: rules `turn_lengths` of
        /// the era the date is in (docs/determinism.md, "Rules per era"). R-TURN-001.</summary>
        public static TurnLength TurnLengthFor(Content c, DateTime date, int rung) =>
            SimTime.ParseLength(c.RulesAt(date).TurnLengths[ClampRung(rung) - 1]);

        static int Weeks(TurnLength len) => len == TurnLength.Week ? 1 : len == TurnLength.Month ? 4 : 13;

        /// <summary>The ladder has rungs 1..5; anything outside is clamped (a bad save or a large order delta).</summary>
        public static int ClampRung(int rung) => Math.Max(1, Math.Min(5, rung));

        /// <summary>The least reserve call-up the rung allows (rules.yaml rung_min_mobilization).</summary>
        public static double MinMobilization(Content c, DateTime date, int rung) => c.RulesAt(date).RungMinMobilization[ClampRung(rung) - 1];

        /// <summary>The call-up a level ends up at on this rung: the sim raises a lower order to the rung's minimum.</summary>
        public static double EffectiveMobilization(Content c, DateTime date, int rung, double level) => Math.Max(level, MinMobilization(c, date, rung));

        /// <summary>Weeks left on the UN ceasefire clock after moving from <paramref name="fromRung"/> (with
        /// <paramref name="weeksLeft"/> on the clock) to <paramref name="toRung"/>: entering full war starts it, leaving ends it.
        /// The GDD loop retires the clock (R-WAR-003, Q-WAR-2 (b)), so there it never starts.</summary>
        public static int CeasefireWeeksAfter(Content c, DateTime date, int fromRung, int weeksLeft, int toRung)
        {
            if (c.IsGdd) return 0;
            toRung = ClampRung(toRung);
            if (toRung != 5) return 0;
            return fromRung != 5 ? c.RulesAt(date).CeasefireWeeks : weeksLeft;
        }

        public static void Init(Content c, GameState s, List<Cause> why) => SetRung(c, s, c.Rules.StartRung, why);

        public static void SetRung(Content c, GameState s, int rung, List<Cause> why)
        {
            rung = ClampRung(rung);
            s.CeasefireWeeksLeft = CeasefireWeeksAfter(c, s.Date, s.Rung, s.CeasefireWeeksLeft, rung);
            if (rung != s.Rung) why.Add(new Cause("security", "rung", rung - s.Rung, FormattableString.Invariant($"Security level {s.Rung} to {rung}")));
            s.Rung = rung;
            RaiseToFloor(c, s, why);
            if (c.IsGdd) SendHomeToFloor(c, s, why);
        }

        /// <summary>Returns null on success, or the reason the order was rejected.</summary>
        public static string? SetMobilization(Content c, GameState s, double level, List<Cause> why)
        {
            if (!(level >= 0 && level <= 1)) return "Mobilization must be between 0 and 1";
            s.Mobilization = level;
            why.Add(new Cause("security", "mobilization", level, $"reserve call-up set to {PlayerText.Percent(level)}"));
            RaiseToFloor(c, s, why);
            return null;
        }

        static void RaiseToFloor(Content c, GameState s, List<Cause> why)
        {
            var rung = ClampRung(s.Rung);   // a bad save cannot index past the table
            var floor = MinMobilization(c, s.Date, rung);
            if (s.Mobilization < floor)
            {
                why.Add(new Cause("security", "mobilization", floor - s.Mobilization, FormattableString.Invariant($"reserve call-up raised to {PlayerText.Percent(floor)}, the minimum for security level {rung}")));
                s.Mobilization = floor;
            }
        }

        /// <summary>R-WAR-002: the GDD loop retires the call-up order, so the call-up follows the rung down to its minimum,
        /// as HistoricalAI's order did (the army demobilised after the 1949 armistices). The debt calibration rests on it
        /// (EconomyCalibrationTests).</summary>
        static void SendHomeToFloor(Content c, GameState s, List<Cause> why)
        {
            var rung = ClampRung(s.Rung);
            var floor = MinMobilization(c, s.Date, rung);
            if (s.Mobilization > floor)
            {
                why.Add(new Cause("security", "mobilization", floor - s.Mobilization, FormattableString.Invariant($"reserves sent home: call-up lowered to {PlayerText.Percent(floor)}, the minimum for security level {rung}")));
                s.Mobilization = floor;
            }
        }

        public static void Apply(Content c, GameState s, TurnLength len, List<Cause> why) => Apply(c, s, Weeks(len), why);

        /// <summary>True while the country is at full war: rung 5, the ladder's top. The war state the sim already keeps (the
        /// ceasefire clock runs only then); D-C4 derives the rung from the war in Phase 5. R-TURN-003 reads it.</summary>
        public static bool AtFullWar(GameState s) => s.Rung == 5;

        /// <summary>Counts the ceasefire clock down by <paramref name="weeks"/> (a whole turn's weeks, or the whole weeks of
        /// an adjusted turn at an era's end) while the country is at full war. The GDD loop retires the 52-week clock
        /// (R-WAR-003): the call is a no-op there (the clock never starts, so it must not count down and impose a ceasefire
        /// at once), and only an event's set_rung moves the rung.</summary>
        public static void Apply(Content c, GameState s, int weeks, List<Cause> why)
        {
            if (c.IsGdd) return;
            if (!AtFullWar(s)) return;
            s.CeasefireWeeksLeft = Math.Max(0, s.CeasefireWeeksLeft - weeks);
            if (s.CeasefireWeeksLeft == 0)
            {
                why.Add(new Cause("security", "ceasefire", 0, "UN ceasefire imposed: full war ends"));   // the rung cause SetRung adds carries the -2
                SetRung(c, s, 3, why);
            }
        }
    }
}
