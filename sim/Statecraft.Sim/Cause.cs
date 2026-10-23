using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    /// <summary>One line of the "Why did this happen?" report.</summary>
    public sealed class Cause
    {
        public string System { get; }
        public string Target { get; }
        public double Delta { get; }
        public string Message { get; }
        /// <summary>What the enemy did, when this cause records it (R-ENEMY-001): Movement.Kind or Orders.Fortify for the order
        /// the enemy AI gave unit Target, or Combat.Attack for an engagement its units led on hex Target (#1297); else empty.
        /// The turn report words it in the player's language (#1293), as Message is English only. A move or fortify the unit
        /// already has is kept with no cause (R-ENEMY-003), so it is named only on the turn it is given; an attack is named on
        /// every turn it engages, as a standing one still does.</summary>
        public string EnemyOrder { get; }
        /// <summary>With EnemyOrder Combat.Attack: the enemy units that attacked in the engagement, in id order; else empty.</summary>
        public IReadOnlyList<int> EnemyAttackers { get; }
        /// <summary>A stable localization key when this line names units (#1399); else empty.</summary>
        public string Key { get; }
        /// <summary>Snapshots of the units this line names, in placeholder order (#1399); else empty.</summary>
        public IReadOnlyList<UnitRef> Units { get; }
        /// <summary>The line's other placeholder values, as invariant text and in placeholder order (#1399); else empty.</summary>
        public IReadOnlyList<string> Args { get; }
        public Cause(string system, string target, double delta, string message, string enemyOrder = "", IReadOnlyList<int>? enemyAttackers = null,
            string key = "", IReadOnlyList<UnitRef>? units = null, IReadOnlyList<string>? args = null)
        {
            System = system; Target = target; Delta = delta; Message = PlayerText.Capitalised(message);   // sentence case (playtest 2, P2-6)
            EnemyOrder = enemyOrder; EnemyAttackers = enemyAttackers ?? Array.Empty<int>();
            Key = key; Units = units ?? Array.Empty<UnitRef>(); Args = args ?? Array.Empty<string>();
        }
        public override string ToString() => $"[{System}] {Message}";
    }
}
