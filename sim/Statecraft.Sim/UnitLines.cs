using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>#1399: a unit as a Why line names it, taken when the line is written, so a unit that then leaves the state
    /// (destroyed, surrendered, sent home) keeps its identity. The UI names it: a starting force by its row, a raised unit by
    /// its type and home city.</summary>
    public sealed class UnitRef
    {
        public int Id { get; }
        public string Type { get; }
        public string Side { get; }
        public string HomeCity { get; }
        /// <summary>#2202: the hex the unit stood on when the sim wrote the line, read from the <see cref="Unit"/> passed in. So
        /// the map can replay a move from the line alone, not from a state that the player's instant attack may already have
        /// changed. Output only: neither hashed nor saved. Per key:
        /// unit_moved (the player's, the enemy's and the route step's moves): the hex before the move; Args[0] is the hex after.
        /// retreated: the hex before the retreat. Only defenders retreat (attackers stay, Q-COMBAT-3), so it is the
        /// engagement's hex; Args[0] is where it went. withdrew: the captured place's hex it left; Args[1] is where it went.
        /// engagement: each unit's own hex, so the attackers' differ from the engagement's hex (Args[0]) and the defenders' match it.
        /// destroyed, surrendered_*: the hex it stood on when it left the state (an attacker's own hex, or the engagement's).
        /// arrived: the hex it reached, as the line is written after the move. fortify_lost: before the move when a move or
        /// an attack writes it, but the hex it has already moved to when the resolver's order step writes it (Args[0] is the
        /// hex it left). Every other key names a unit that does not move on that line, so it is the unit's hex.</summary>
        public int Q { get; }
        public int R { get; }

        public UnitRef(Unit u)
        {
            Id = u.Id; Type = u.Type; Side = u.Side; HomeCity = u.HomeCity; Q = u.Q; R = u.R;
        }
    }

    /// <summary>#1399: the keys of the Why lines that name units; the UI words each as why.line.&lt;key&gt;.</summary>
    public static class WhyKeys
    {
        public const string UnitMoved = "unit_moved", FortifyLost = "fortify_lost", Fortifying = "fortifying", Holds = "holds",
            AttackEndedTruce = "attack_ended_truce", AttackOrdered = "attack_ordered", AttackEnded = "attack_ended",
            HoldsFire = "holds_fire", AttackCalledOff = "attack_called_off", Engagement = "engagement", Destroyed = "destroyed",
            Retreated = "retreated", SurrenderedPrisoners = "surrendered_prisoners", SurrenderedHeld = "surrendered_held",
            SentHome = "sent_home", Withdrew = "withdrew", OnItsWay = "on_its_way",
            StoppedNoRoute = "stopped_no_route", StoppedFull = "stopped_full", Arrived = "arrived",
            CardOrderEnded = "card_order_ended";

        public static readonly IReadOnlyList<string> All = new[]
        {
            UnitMoved, FortifyLost, Fortifying, Holds, AttackEndedTruce, AttackOrdered, AttackEnded, HoldsFire,
            AttackCalledOff, Engagement, Destroyed, Retreated, SurrenderedPrisoners, SurrenderedHeld, SentHome,
            Withdrew, OnItsWay, StoppedNoRoute, StoppedFull, Arrived, CardOrderEnded,
        };
    }

    static class UnitLines
    {
        /// <summary>A map line naming <paramref name="units"/> (its target the first one's id), with its key and values.</summary>
        internal static Cause Line(string key, string message, IReadOnlyList<Unit> units, params string[] args) =>
            new Cause("map", units[0].Id.ToString(CultureInfo.InvariantCulture), 0, message,
                key: key, units: units.Select(u => new UnitRef(u)).ToList(), args: args);
    }
}
