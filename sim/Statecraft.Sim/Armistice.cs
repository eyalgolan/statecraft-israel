using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>#1709 slice A, docs/rules/mvp.md R-WAR-005 to R-WAR-007, under exact gdd-0.3 only (gdd-0.1's removal is #752's):
    /// each front's quiet-turn count, whether a front may get an early armistice offer, and what signing does to that front's
    /// units. The fronts and the sides on them are Prisoners.FrontOf's (Iraq under Jordan; the ALA and local forces on none).
    /// The early offer card itself is slice B. Draws nothing.</summary>
    internal static class Armistice
    {
        /// <summary>The owner's ruling on #1709 (27 October 2026, "15 December turn (Recommended)"): "The first offer can come on
        /// the 15 December 1948 turn, not earlier."</summary>
        internal static readonly DateTime EarliestOffer = new DateTime(1948, 12, 15);

        /// <summary>R-WAR-005: the front's consecutive quiet turns; 0 before the first turn end and under every other ruleset.</summary>
        internal static int QuietTurns(GameState s, string front) => s.QuietTurns.TryGetValue(front, out var n) ? n : 0;

        /// <summary>R-WAR-005, R-WAR-006: an early offer is eligible for an unsigned front from the 15 December 1948 turn once it
        /// has been quiet for military.armistice_quiet_turns turns, counted again from 0 after a refusal.</summary>
        internal static bool MayOfferEarly(Content c, GameState s, string front) =>
            Gdd03Military.Applies(c) && s.Date >= EarliestOffer && !Demobilization.Signed(s, front)
            && QuietTurns(s, front) >= Gdd03Military.Scalar(c, "military.armistice_quiet_turns");

        /// <summary>R-WAR-005 (Q48): an engagement breaks the count of every front with a unit in it, whoever attacked. Called by
        /// Combat.Engage, so a refused order, which never engages, breaks nothing.</summary>
        internal static void Engaged(Content c, GameState s, IEnumerable<Unit> units)
        {
            if (!Gdd03Military.Applies(c)) return;
            foreach (var u in units)
                if (Prisoners.FrontOf.TryGetValue(u.Side, out var front)) s.EngagedFronts.Add(front);
        }

        /// <summary>R-WAR-005 (Q48), at the turn's end: each front not engaged this turn gains a quiet turn, each engaged front
        /// goes back to 0, and the turn's engagements are forgotten.</summary>
        internal static void CountQuietTurns(Content c, GameState s)
        {
            if (!Gdd03Military.Applies(c)) return;
            foreach (var front in Prisoners.Fronts)
                s.QuietTurns[front] = s.EngagedFronts.Contains(front) ? 0 : QuietTurns(s, front) + 1;
            s.EngagedFronts.Clear();
        }

        /// <summary>R-WAR-006, R-WAR-007: the armistice card of a front (content/events/armistice_&lt;front&gt;.yaml) resolved.
        /// hold_out is the refusal (Q49: its front counts again from 0 after the refusal's own turn, #1709 open question 4); sign removes the front's units.
        /// Gated here on the ruleset, so #752 can open the removal to gdd-0.1 by calling <see cref="RemoveFront"/>.</summary>
        internal static void Resolved(Content c, GameState s, string eventId, string optionId)
        {
            if (!Gdd03Military.Applies(c) || !eventId.StartsWith("armistice_", StringComparison.Ordinal)) return;
            var front = eventId.Substring("armistice_".Length);
            if (!Prisoners.Fronts.Contains(front)) return;
            if (optionId == "hold_out")
            {
                // Q49 (owner, 1 November 2026): "Does the refusal turn count toward the next quiet period? No. Two full quiet
                // turns must follow the refusal." The card is answered before its turn resolves, so the front is also marked
                // as broken for this turn: CountQuietTurns ends the turn at 0 and T+1, T+2 quiet make it eligible on T+3.
                s.QuietTurns[front] = 0;
                s.EngagedFronts.Add(front);
            }
            else if (optionId == "sign") RemoveFront(s, front);
        }

        /// <summary>R-WAR-007 (Q-OBJ-4): on a front's armistice its units leave the board. Prisoners held and the player's
        /// soldiers held stay as they are: the exchange cards return them (R-POW-004). Checks no ruleset.</summary>
        internal static void RemoveFront(GameState s, string front)
        {
            foreach (var id in s.Units.Values.Where(u => Prisoners.FrontOf.TryGetValue(u.Side, out var f) && f == front).Select(u => u.Id).ToList())
                s.Units.Remove(id);
        }
    }
}
