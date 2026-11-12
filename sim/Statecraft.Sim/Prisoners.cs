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
        /// side (<see cref="CaptureRecords"/>), and SoldiersHeld grows by the records captured, so the count per captor stays
        /// the Captured records with that captor (owner, 8 November 2026: "Keep count + check (Recommended)"). Without a
        /// ledger it grows by the unit's strength, as before. Enemy prisoners stay a count per side (owner, 8 November 2026:
        /// "No, keep count (Recommended)"). A synthetic or Open ledger, an unbound captor and a held count that already
        /// disagrees with the records are refused; a refusal throws before Take writes anything, and what the throw leaves in
        /// combat is set out at Combat.Fight's call. Returns the Why line's last sentence; Held, the population units taken
        /// (the records captured on a ledger, owner, 9 November 2026: "Records captured (Recommended)"; else the unit's
        /// strength), which the surrender Why line states in people; and, when a formation's leftover records went home
        /// ("Sent home (Recommended)"), their city (Home), their number (Sent) and the Arms they returned to the stock
        /// (ArmsBack), else null, 0 and 0.</summary>
        internal static (string Text, int Held, int ArmsBack, string? Home, int Sent) Take(Content c, GameState s, Unit u, Unit captor)
        {
            PopulationLedger.RefuseUnlessHistorical(s);
            if (s.PopulationLedger.IsInitialized) RequireHeldMatchesRecords(s);
            if (u.Side == Movement.PlayerSide)
            {
                int back = 0, sent = 0; string? home = null;
                var held = s.PopulationLedger.IsInitialized ? CaptureRecords(c, s, u, captor.Side, out back, out home, out sent) : u.Strength;
                Add(s.SoldiersHeld, captor.Side, held);
                return ("They are held by the enemy", held, back, home, sent);
            }
            Add(s.PrisonersHeld, u.Side, u.Strength);
            return ("They are held as prisoners", u.Strength, 0, null, 0);
        }

        /// <summary>R-POW-005, R-POP-002 on the dark start: the serving soldier records of the player's formation
        /// <paramref name="u"/> become Captured under <paramref name="side"/>, in one validated ledger commit, and the number
        /// captured is returned. They are the active soldiers on its ActiveService target, found as battles find them
        /// (<see cref="Training.FormationTarget"/>: an opening formation's units_1948 target, a trained one's formation/&lt;id&gt;;
        /// owner, 9 November 2026: "Yes, in #1706 (Recommended)"). A strength its records do not number is not refused (owner,
        /// 9 November 2026: "Go ahead on strength (Recommended)"): with more records than its strength, its strength of them
        /// are captured, the lowest ids first; with fewer, all of them are. The leftover records leave service as soldiers sent
        /// home do (owner, 9 November 2026: "Sent home (Recommended)"; R-UNIT-005): Demobilization's
        /// <see cref="Demobilization.RecordsHome"/>, Send home's own ledger code, written in the capture's one ledger commit,
        /// make them settled-in free civilians without an assignment in the nearest city to the formation's hex
        /// (<see cref="Demobilization.Nearest"/>) and return their issued items to the Arms stock (<see
        /// cref="Demobilization.ReturnArms"/>); <paramref name="back"/> is the Arms added, <paramref name="home"/> that city and
        /// <paramref name="sent"/> their number (0, null and 0 with no leftover). The captives and the leftovers change in
        /// one validated ledger commit (PopulationLedger.Capture with the leftovers as replacements), so every check comes
        /// before any write: a missing region or reception.turns in RecordsHome, then an unbound captor, a record that is not
        /// an active soldier, or a leftover location the ledger does not bind, all refused with nothing written. The Arms go
        /// back after that commit.</summary>
        static int CaptureRecords(Content c, GameState s, Unit u, string side, out int back, out string? home, out int sent)
        {
            var key = Training.FormationTarget(c, u.Id);
            var serving = s.PopulationLedger.Units.Where(p => p.Status == PopulationStatus.ActiveSoldier
                    && p.Assignment.Kind == PopulationAssignmentKind.ActiveService && p.Assignment.Target == key)
                .OrderBy(p => p.Id).ToList();
            var take = Math.Max(0, u.Strength);
            var ids = serving.Take(take).Select(p => p.Id).ToList();
            var leftover = serving.Skip(take).ToList();
            back = 0; home = null; sent = 0;
            List<PopulationUnit>? civilians = null; var items = 0;
            if (leftover.Count > 0)
            {
                home = Demobilization.Nearest(c, u.Q, u.R);
                civilians = Demobilization.RecordsHome(c, leftover, home, out items);
            }
            s.PopulationLedger.Capture(ids, side, civilians);
            if (civilians != null) { sent = civilians.Count; back = Demobilization.ReturnArms(s, items); }
            return ids.Count;
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

        /// <summary>#2473, R-POW-005, R-POP-002 (owner, 9 November 2026, recorded on #2473): the Captured records held by
        /// <paramref name="front"/>'s sides, in ascending population id, come home under the same ids as Send home's records
        /// leave service (<see cref="Demobilization.RecordsHome"/>): free civilians without an assignment, captor or home,
        /// "Yes, settled in (Recommended)" (reception ticks reception.turns), "Keep both (Recommended)" (qualification and
        /// combat XP kept). "It's gone (Recommended)": the issued military item leaves the record and nothing is added to Arms
        /// (RecordsHome's item count is not returned). "Nearest to region label (Recommended)": each goes to the nearest census
        /// city to its home region's label place (regions.yaml capital_location), by Send home's distance rule and city-id
        /// tie (<see cref="Demobilization.Nearest"/>). One validated ledger commit, so a refusal writes nothing.</summary>
        static void ReturnRecords(Content c, GameState s, string front)
        {
            var sides = new HashSet<string>(Sides(front), StringComparer.Ordinal);
            var captives = s.PopulationLedger.Units.Where(p => p.Status == PopulationStatus.Captured && sides.Contains(p.Captor!))
                .OrderBy(p => p.Id).ToList();
            if (captives.Count == 0) return;
            var civilians = new List<PopulationUnit>();
            foreach (var g in captives.GroupBy(p => p.Home!.Region, StringComparer.Ordinal))
            {
                var label = c.PlayerRegions.Regions.TryGetValue(g.Key, out var region) ? region.CapitalHex
                    : throw new InvalidOperationException("No region in regions.yaml for " + g.Key + " (R-POW-005).");
                var released = g.Select(p => p.WithPlacement(p.Status, p.Location, p.Assignment, null, null));
                civilians.AddRange(Demobilization.RecordsHome(c, released, Demobilization.Nearest(c, label.Q, label.R), out _));
            }
            s.PopulationLedger.Commit(civilians, Array.Empty<PopulationTarget>());
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
        /// score (Q-POW-4). On an initialized Historical ledger (#2473) the Captured records that front's sides hold come home
        /// too, by <see cref="ReturnRecords"/>, before any other write; a synthetic or Open ledger is still refused.</summary>
        internal static void Return(Content c, GameState s, string front, List<Cause> why)
        {
            PopulationLedger.RefuseUnlessHistorical(s);
            if (s.PopulationLedger.IsInitialized) { RequireHeldMatchesRecords(s); ReturnRecords(c, s, front); }
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
