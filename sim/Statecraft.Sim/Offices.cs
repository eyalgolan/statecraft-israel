using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Statecraft.Sim
{
    /// <summary>"Who held this office / which body sat on this date." Periods are [from, to).</summary>
    public static class Offices
    {
        static bool In(DateTime d, DateTime from, DateTime? to) => d >= from && (to == null || d < to.Value);

        /// <summary>The historical holder on date. A holder has left office from their date of death on, even when the
        /// term is open-ended (the loader lets an open term outlive a death inside the latest government, #167).</summary>
        public static Person? Holder(Content c, string office, DateTime date) =>
            c.People.FirstOrDefault(p => (!p.Died.HasValue || date < p.Died.Value) && p.Offices.Any(t => t.Office == office && In(date, t.From, t.To)));

        /// <summary>Party of the person on date: the party of a term held that day, else Person.Party.</summary>
        public static string PartyOf(Content c, Person p, DateTime date) =>
            p.Offices.FirstOrDefault(t => t.Party != "" && In(date, t.From, t.To))?.Party ?? p.Party;

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
