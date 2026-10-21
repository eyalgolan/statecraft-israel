using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Settlement control in the GDD v0.1 loop (GDD Phase 6; docs/gdd/settlement-control.md). This half holds resolver
    /// step 3, the capture check (R-CTRL-004, R-CTRL-005). The record itself is in GddState.cs.</summary>
    public sealed partial class SettlementControl
    {
        /// <summary>The <see cref="State"/> of a target the player has captured and has not yet chosen an outcome for.</summary>
        public const string AwaitingChoice = "awaiting_choice";

        /// <summary>Resolver step 3 (R-CTRL-004, R-CTRL-005), after combat, in ordinal settlement id. A curated target that
        /// starts under enemy control (on mvp.enemy_held_localities, owner, 21 October 2026, #796) is captured when the
        /// player's military units are on its hex and no enemy military unit is (Q-CAP-1 (a); Q-CAP-3 (a): only enemy military
        /// units are combatants; #796: a halutz holds nothing): its record names the player, awaits the player's choice and
        /// starts on <paramref name="next"/>, the turn's end. A captured target still awaiting the choice that only enemy
        /// military units are on is under enemy control again, and its record goes (#796). A target that is not enemy-held
        /// at the start (Jaffa, #793) is never captured. Capture writes the settlement state only: no resident field, unit,
        /// resource, score or objective (R-CTRL-005). It draws nothing from the RNG.</summary>
        public static void Capture(Content c, GameState s, DateTime next, List<Cause> why)
        {
            var enemyHeld = new HashSet<string>(c.RulesAt(s.Date).Mvp!.EnemyHeldLocalities, StringComparer.Ordinal);
            foreach (var t in c.SettlementTargets.Where(x => enemyHeld.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                var holder = Holder(s, t.Q, t.R);
                var record = s.Settlements.TryGetValue(t.Id, out var held) ? held : null;
                var name = c.SettlementNames[t.Id];
                if (holder == Movement.PlayerSide && record == null)
                {
                    s.Settlements[t.Id] = new SettlementControl(Movement.PlayerSide, AwaitingChoice, next);
                    why.Add(new Cause("map", t.Id, 0, $"{name} is captured: Israeli units hold it and no enemy unit is there. It awaits a decision"));
                }
                else if (holder == Enemy && record?.State == AwaitingChoice)   // PR 5's outcomes decide what a later state returns to
                {
                    s.Settlements.Remove(t.Id);
                    why.Add(new Cause("map", t.Id, 0, $"{name} is under enemy control again: enemy units hold it and no Israeli unit is there"));
                }
            }
        }

        const string Enemy = "enemy";

        /// <summary>Who holds (q, r): <see cref="Movement.PlayerSide"/> when only Israeli military units are on it,
        /// <see cref="Enemy"/> when only enemy ones are, else null (none, or both).</summary>
        static string? Holder(GameState s, int q, int r)
        {
            var on = s.Units.Values.Where(u => u.Q == q && u.R == r && u.Side != "").ToList();
            if (on.Count == 0) return null;
            var player = on.Count(u => u.Side == Movement.PlayerSide);
            return player == on.Count ? Movement.PlayerSide : player == 0 ? Enemy : null;
        }
    }
}
