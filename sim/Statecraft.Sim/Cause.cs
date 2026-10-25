using System;
using System.Collections.Generic;
using System.Globalization;

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

    /// <summary>B1: the keys of the resource, population, effect and security lines, so the UI words them in the player's
    /// language (why.cause.&lt;key&gt;) from the line's Target, Delta and Args instead of its English Message. Message stays
    /// as it was, word for word, for the CLI, the tests and every line without a key. These lines name no unit, so they are
    /// apart from <see cref="WhyKeys"/>. A line that is not keyed yet keeps its English Message on screen.</summary>
    public static class CauseKeys
    {
        /// <summary>Target money moved by Delta. Args: tax, spending, reserve call-up (Economy).</summary>
        public const string MoneyFlows = "money_flows";
        /// <summary>Foreign currency moved by Delta. Args: exports, donations and loans, imports.</summary>
        public const string FxFlows = "fx_flows";
        /// <summary>Delta borrowed into the debt. Args: what for (<see cref="Deficit"/> or <see cref="CivilianUpkeep"/>).</summary>
        public const string Borrowed = "borrowed";
        /// <summary>Target money moved by Delta, for civilian upkeep. No args.</summary>
        public const string CivilianUpkeep = "civilian_upkeep";
        /// <summary>The Borrowed purpose for the end-of-turn deficit.</summary>
        public const string Deficit = "deficit";
        public const string FxCrisisBegan = "fx_crisis_began", FxCrisisEased = "fx_crisis_eased";
        /// <summary>Target (food_supply) moved by Delta while the foreign currency crisis cuts food imports. No args.</summary>
        public const string FxCrisisFood = "fx_crisis_food";
        /// <summary>Delta immigrants arrived from the origin group Target. No args.</summary>
        public const string Arrived = "immigrants_arrived";
        /// <summary>Indicator Target moved by Delta. Args: the ids of the effects that moved it, in content order (none: no
        /// effect named).</summary>
        public const string EffectChange = "effect_change";
        /// <summary>Situation Target began or ended. Args: its indicator, the indicator's level.</summary>
        public const string SituationBegan = "situation_began", SituationEnded = "situation_ended";
        /// <summary>The security level changed. Args: from, to.</summary>
        public const string RungChanged = "rung_changed";
        /// <summary>The call-up was set to Delta. No args.</summary>
        public const string CallUpSet = "call_up_set";
        /// <summary>The call-up was raised or lowered to a level's minimum. Args: the new call-up, the security level.</summary>
        public const string CallUpRaised = "call_up_raised", CallUpLowered = "call_up_lowered";
        public const string Ceasefire = "ceasefire";

        public static readonly IReadOnlyList<string> All = new[]
        {
            MoneyFlows, FxFlows, Borrowed, CivilianUpkeep, FxCrisisBegan, FxCrisisEased, FxCrisisFood, Arrived, EffectChange,
            SituationBegan, SituationEnded, RungChanged, CallUpSet, CallUpRaised, CallUpLowered, Ceasefire,
        };

        /// <summary>A number as an Arg: invariant and round-trip, so the UI rounds exactly the value Message rounded.</summary>
        internal static string Number(double v) => v.ToString("R", CultureInfo.InvariantCulture);
        internal static string Number(int v) => v.ToString(CultureInfo.InvariantCulture);
    }
}
