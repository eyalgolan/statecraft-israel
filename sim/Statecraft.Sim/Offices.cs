using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Statecraft.Sim
{
    /// <summary>"Who held this office / which body sat on this date." Periods are [from, to).</summary>
    public static class Offices
    {
        static bool In(DateTime d, DateTime from, DateTime? to) => d >= from && (to == null || d < to.Value);

        /// <summary>The historical holder on date; for an office with several_holders, the first in people.yaml order
        /// (the loader keeps such offices out of event text, policies and effects, so nothing asks it for one). A holder has left office from their date of death on, even when the
        /// term is open-ended (the loader lets an open term outlive a death inside the latest government, #167).</summary>
        public static Person? Holder(Content c, string office, DateTime date) =>
            c.People.FirstOrDefault(p => (!p.Died.HasValue || date < p.Died.Value) && p.Offices.Any(t => t.Office == office && In(date, t.From, t.To)));

        /// <summary>Everyone holding the office on date, in people.yaml order (one person, or none, unless the office
        /// has several_holders: true). <see cref="Holder"/> is the first of these.</summary>
        public static System.Collections.Generic.IReadOnlyList<Person> Holders(Content c, string office, DateTime date) =>
            c.People.Where(p => (!p.Died.HasValue || date < p.Died.Value) && p.Offices.Any(t => t.Office == office && In(date, t.From, t.To))).ToList();

        /// <summary>Party of the person on date: the party of a term held that day, else Person.Party.</summary>
        public static string PartyOf(Content c, Person p, DateTime date) =>
            p.Offices.FirstOrDefault(t => t.Party != "" && In(date, t.From, t.To))?.Party ?? p.Party;

        /// <summary>The office that owns budget line <paramref name="line"/> on date (#108): the last term of rules.yaml
        /// budget_owners that has begun, or, for a line with no entry, the office with the line's own id. Null when the
        /// line does not exist or its first term has not begun. Lines are start-only, so this reads the base rules.</summary>
        public static string? BudgetOwner(Content c, string line, DateTime date)
        {
            if (!c.Rules.BudgetStart.ContainsKey(line)) return null;
            if (!c.Rules.BudgetOwners.TryGetValue(line, out var terms)) return line;
            string? owner = null;
            foreach (var t in terms.OrderBy(t => t.From))
                if (t.From <= date) owner = t.Office;
            return owner;
        }

        /// <summary>The budget line that <paramref name="office"/> owns on date, or null when it owns none (the loader allows
        /// at most one). Lines are searched in ordinal order, so the answer never depends on dictionary order.</summary>
        public static string? BudgetLineOf(Content c, string office, DateTime date) =>
            c.Rules.BudgetStart.Keys.OrderBy(k => k, StringComparer.Ordinal).FirstOrDefault(l => BudgetOwner(c, l, date) == office);

        public static Legislature? Legislature(Content c, DateTime date) =>
            c.Legislatures.FirstOrDefault(l => In(date, l.From, l.To));

        public static Government? Government(Content c, DateTime date) =>
            c.Governments.FirstOrDefault(g => In(date, g.From, g.To));

        internal static readonly Regex Placeholder = new Regex(@"\{([a-z0-9_]+)\}");

        internal static readonly Regex OfficeId = new Regex(@"^[a-z][a-z0-9_]*\z");

        /// <summary>Replaces {office_id} with the name of the person holding it on date.</summary>
        public static string Fill(Content c, string text, DateTime date) =>
            Placeholder.Replace(text, m =>
                Holder(c, m.Groups[1].Value, date)?.Name
                ?? throw new InvalidOperationException(FormattableString.Invariant($"no holder of '{m.Groups[1].Value}' on {date:yyyy-MM-dd}")));
    }
}
