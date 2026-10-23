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

        public UnitRef(Unit u)
        {
            Id = u.Id; Type = u.Type; Side = u.Side; HomeCity = u.HomeCity;
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
            StoppedNoRoute = "stopped_no_route", StoppedFull = "stopped_full", Arrived = "arrived";

        public static readonly IReadOnlyList<string> All = new[]
        {
            UnitMoved, FortifyLost, Fortifying, Holds, AttackEndedTruce, AttackOrdered, AttackEnded, HoldsFire,
            AttackCalledOff, Engagement, Destroyed, Retreated, SurrenderedPrisoners, SurrenderedHeld, SentHome,
            Withdrew, OnItsWay, StoppedNoRoute, StoppedFull, Arrived,
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
