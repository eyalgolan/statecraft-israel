using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Prisoners in the GDD v0.1 loop (Phase 5 PR 12; docs/rules/mvp.md R-POW-001 to R-POW-005), the impact plan's
    /// section 4.3 as written (Q-POW-1, owner, 15 October 2026). Only a surrender in combat takes prisoners; residents never
    /// are. They are held on Food upkeep and are never labour, Money, Arms or anything a command or event can spend. Each
    /// front's sourced exchange card returns them, and the points come then, never at the capture. The player's soldiers
    /// taken in a surrender are a count that costs Money while held and comes home on the same card, with no score. Nothing
    /// here celebrates a capture: the Why lines state counts in people, as combat states its losses (Q-CIV-4).</summary>
    public static class Prisoners
    {
        /// <summary>The front whose exchange card returns each enemy side's prisoners: the armistice agreements' parties
        /// (Q-OBJ-1), with Iraq under Jordan, whose agreement took over the Iraqi sector (Art. VI). The ALA and the local
        /// forces belong to no front, so no card returns their prisoners (they stay held to the game's end).</summary>
        internal static readonly Dictionary<string, string> FrontOf = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["egypt"] = "egypt", ["lebanon"] = "lebanon", ["syria"] = "syria", ["jordan"] = "jordan", ["iraq"] = "jordan",
        };

        /// <summary>The fronts an exchange card may name (GameEvent.Prisoners), in ordinal order.</summary>
        public static readonly IReadOnlyList<string> Fronts = FrontOf.Values.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();

        static string Name(string front) => PlayerText.Capitalised(front);

        static void Add(SortedDictionary<string, int> m, string side, int units) =>
            m[side] = (m.TryGetValue(side, out var n) ? n : 0) + units;

        /// <summary>R-POW-001, R-POW-005: unit <paramref name="u"/> surrendered to <paramref name="captor"/>'s side. An enemy unit's
        /// people become prisoners under its own side; the player's soldiers are counted under the captor's side. Called only by
        /// Combat's surrender, the one way a unit is captured. With an initialized Historical ledger (the dark start, #1710,
        /// #1725 PR 7) the player's surrendering formation's serving soldier records also become Captured under the captor's
        /// side (<see cref="CaptureRecords"/>), checked in full before anything is written; enemy prisoners stay a count per
        /// side (owner, 8 November 2026: "No, keep count (Recommended)"). A synthetic or Open ledger is still refused. A refusal
        /// throws before Take writes anything; what the throw leaves in combat is set out at Combat.Fight's call.</summary>
        internal static string Take(Content c, GameState s, Unit u, Unit captor)
        {
            PopulationLedger.RefuseUnlessHistorical(s);
            if (s.PopulationLedger.IsInitialized) RequireHeldMatchesRecords(s);
            if (u.Side == Movement.PlayerSide)
            {
                if (s.PopulationLedger.IsInitialized) CaptureRecords(c, s, u, captor.Side);
                Add(s.SoldiersHeld, captor.Side, u.Strength);
                return "They are held by the enemy";
            }
            Add(s.PrisonersHeld, u.Side, u.Strength);
            return "They are held as prisoners";
        }

        /// <summary>R-POW-005, R-POP-002 on the dark start: the opening formation <paramref name="u"/>'s serving soldier records
        /// (active soldiers on its units_1948 target; unit id i is c.StartingForces[i], StartingForces.Init) become Captured
        /// under <paramref name="side"/>, in one validated ledger commit. Refused before any write when the unit is not a
        /// units_1948 formation (a trained formation's soldiers serve a target this ledger does not have yet) or when its
        /// records do not number its strength (R-POP-004).</summary>
        static void CaptureRecords(Content c, GameState s, Unit u, string side)
        {
            if (u.Id < 0 || u.Id >= c.StartingForces.Count || c.StartingForces[u.Id].Side != Movement.PlayerSide)
                throw new InvalidOperationException(FormattableString.Invariant(
                    $"Unit {u.Id} is not a formation of units_1948.yaml, so its soldiers have no ledger records to capture (R-POP-002)."));
            var key = HistoricalPopulation.FormationPrefix + c.StartingForces[u.Id].Row;
            var ids = s.PopulationLedger.Units.Where(p => p.Status == PopulationStatus.ActiveSoldier
                    && p.Assignment.Kind == PopulationAssignmentKind.ActiveService && p.Assignment.Target == key)
                .Select(p => p.Id).OrderBy(x => x).ToList();
            if (ids.Count != u.Strength)
                throw new InvalidOperationException(FormattableString.Invariant(
                    $"Unit {u.Id} has strength {u.Strength} but the population ledger serves it with {ids.Count} (R-POP-004)."));
            s.PopulationLedger.Capture(ids, side);
        }

        /// <summary>The owner's ruling of 8 November 2026, "Keep count + check (Recommended)": on an initialized ledger,
        /// SoldiersHeld per captor side equals the Captured records with that captor. Read only; a mismatch is refused.</summary>
        internal static void RequireHeldMatchesRecords(GameState s)
        {
            var records = s.PopulationLedger.Units.Where(p => p.Status == PopulationStatus.Captured)
                .GroupBy(p => p.Captor!, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
            foreach (var side in records.Keys.Union(s.SoldiersHeld.Keys, StringComparer.Ordinal))
                if ((records.TryGetValue(side, out var n) ? n : 0) != (s.SoldiersHeld.TryGetValue(side, out var m) ? m : 0))
                    throw new InvalidOperationException(FormattableString.Invariant(
                        $"Our soldiers held by {side} number {m} but the population ledger has {n} captured records with that captor (R-POW-005)."));
        }

        static IEnumerable<string> Sides(string front) => FrontOf.Where(kv => kv.Value == front).Select(kv => kv.Key).OrderBy(x => x, StringComparer.Ordinal);

        /// <summary>True while <paramref name="front"/> holds the player's soldiers or the player holds its prisoners: its exchange
        /// card waits for this (R-POW-004), so a game with no surrender fires no card. A stored 0 (a zero-strength surrender)
        /// holds nobody, so only a positive count counts; the entry itself stays, so saved state does not change.</summary>
        internal static bool Held(GameState s, string front) =>
            Sides(front).Any(x => s.PrisonersHeld.TryGetValue(x, out var n) && n > 0 || s.SoldiersHeld.TryGetValue(x, out var m) && m > 0);

        /// <summary>R-POW-002, R-POW-005, step 7 after the nation eats and the import: the prisoners eat food_per_prisoner_unit a
        /// unit from the stock, as far as it goes; each Food unit it cannot give costs prisoner_shortfall_cohesion of Social
        /// cohesion and never a prisoner (Q-POW-5). Then the Treasury pays held_soldier_cost_per_turn for each unit of the
        /// player's soldiers held, borrowing what it lacks (Q-POW-4; R-MONEY-003). On an initialized Historical ledger (#1710,
        /// #1725 PR 7) it runs as written, from the same counts: the held soldiers keep their Money cost (owner, 8 November
        /// 2026: "Yes, as today (Recommended)"), the captured own records are not fed here (they eat the national ration), and
        /// no record is read or written beyond <see cref="RequireHeldMatchesRecords"/>'s check, made before any write.</summary>
        public static void Upkeep(Content c, GameState s, MvpRules r, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);
            if (s.PopulationLedger.IsInitialized) RequireHeldMatchesRecords(s);
            var need = (long)s.PrisonersHeld.Values.Sum() * r.FoodPerPrisonerUnit!.Value;
            if (need > 0)
            {
                var eaten = (int)Math.Min(need, s.Food);
                s.Food -= eaten;
                why.Add(new Cause("economy", "food", -eaten, FormattableString.Invariant($"The prisoners held ate {eaten} units of Food"), key: CauseKeys.PrisonerFood));
                var unfed = need - eaten;
                if (unfed > 0)
                {
                    var change = EffectsSystem.Shift(c, s, "morale", -unfed * r.PrisonerShortfallCohesion!.Value);
                    why.Add(new Cause("economy", "morale", change, FormattableString.Invariant($"The prisoners held went short of {unfed} units of Food: {PlayerText.Change(c, "morale", change)}"), key: CauseKeys.PrisonerFoodShortage, args: new[] { CauseKeys.Number(unfed) }));
                }
            }
            var held = s.SoldiersHeld.Values.Sum();
            if (held == 0) return;
            var cost = held * r.HeldSoldierCostPerTurn!.Value;
            if (cost > 0) EconomySystem.PayOrBorrow(c, s, cost, "for our soldiers held by the enemy", why, CauseKeys.Purpose.Prisoners);
        }

        /// <summary>R-POW-004, R-POW-005: <paramref name="front"/>'s exchange card was resolved. Its sides' prisoners go home and
        /// score points_per_prisoner_unit_returned a unit (Q-POW-2); the player's soldiers that front held come home, with no
        /// score (Q-POW-4).</summary>
        internal static void Return(Content c, GameState s, string front, List<Cause> why)
        {
            PopulationLedger.RefuseAggregate(s);
            // Only GDD combat creates prisoners and the card waits for them, so this runs only in the GDD loop, which has Mvp.
            var points = c.RulesAt(s.Date).Mvp!.PointsPerPrisonerUnitReturned!.Value;
            var ppu = c.RulesAt(s.Date).Mvp!.PeoplePerUnit;
            int returned = 0, home = 0;
            foreach (var side in Sides(front))
            {
                if (s.PrisonersHeld.TryGetValue(side, out var n)) { s.PrisonersHeld.Remove(side); Add(s.PrisonersReturned, side, n); returned += n; }
                if (s.SoldiersHeld.TryGetValue(side, out var m)) { s.SoldiersHeld.Remove(side); home += m; }
            }
            if (returned > 0)
            {
                s.Score += returned * points;
                s.ScorePrisonerPoints += returned * points;   // R-SCORE-001
                why.Add(new Cause("events", "prisoners", returned, FormattableString.Invariant($"Prisoners exchanged: {returned * ppu:N0} prisoners returned to {Name(front)}"),
                    key: CauseKeys.PrisonersReturned, args: new[] { CauseKeys.Number(returned * ppu), front }));
            }
            if (home > 0)
                why.Add(new Cause("events", "prisoners", home, FormattableString.Invariant($"Prisoners exchanged: {home * ppu:N0} of our soldiers came home from {Name(front)}"),
                    key: CauseKeys.SoldiersReturned, args: new[] { CauseKeys.Number(home * ppu), front }));
        }
    }
}
