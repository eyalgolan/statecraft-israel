using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>A player appointment: <see cref="Person"/> holds the office from <see cref="From"/> while the government
    /// that was in office then still is. Immutable, so GameState.Clone can copy the dictionary shallowly.</summary>
    public sealed class Appointment
    {
        public string Person { get; }
        public DateTime From { get; }
        public Appointment(string person, DateTime from) { Person = person; From = from; }
    }

    /// <summary>One office and who holds it now.</summary>
    public sealed class Seat
    {
        public Office Office { get; }
        public Person? Holder { get; }
        /// <summary>Everyone holding the office today: one person or none, except for an office with several_holders.</summary>
        public IReadOnlyList<Person> Holders { get; }
        /// <summary>True when the holder is the player's appointee, not the historical minister.</summary>
        public bool Appointed { get; }
        public Seat(Office office, Person? holder, bool appointed, IReadOnlyList<Person>? holders = null)
        {
            Office = office; Holder = holder; Appointed = appointed;
            Holders = holders ?? (holder == null ? new Person[0] : new[] { holder });
        }
    }

    /// <summary>A Cabinet refusal's stable UI kind and invariant values, beside the unchanged English text used by commands.</summary>
    public sealed class CabinetReason
    {
        public string Kind { get; }
        public IReadOnlyList<string> Values { get; }
        public string Text { get; }
        public CabinetReason(string kind, string text, params string[] values)
        {
            Kind = kind; Text = text; Values = values;
        }
    }

    /// <summary>The cabinet as the game sees it: the historical holders from people.yaml, overridden by the player's
    /// appointments until the government changes (a new government takes office with its own, historical, ministers).</summary>
    public static class Cabinet
    {
        public const string Kind = "appoint";

        /// <summary>The player's appointee for the office on the date: appointed by then, same government, and alive (an
        /// appointee who dies leaves office on the date of death, and the historical holder applies again).</summary>
        static Appointment? Active(Content c, GameState s, string office, DateTime date)
        {
            if (c.Offices.Any(o => o.Id == office && (o.HistoricalOnly || !o.ExistsOn(date)))) return null;
            if (!s.Appointments.TryGetValue(office, out var a) || date < a.From) return null;
            var died = c.People.First(p => p.Id == a.Person).Died;
            if (died.HasValue && date >= died.Value) return null;
            return Offices.Government(c, date)?.Id == s.AppointedUnder ? a : null;
        }

        /// <summary>Who holds <paramref name="office"/> on <paramref name="date"/> (default: the state's date).</summary>
        public static Person? Holder(Content c, GameState s, string office, DateTime? date = null)
        {
            var d = date ?? s.Date;
            var a = Active(c, s, office, d);
            return a != null ? c.People.First(p => p.Id == a.Person) : Offices.Holder(c, office, d);
        }

        /// <summary>Offices.Fill with the player's appointments: {office_id} becomes the name of whoever holds it.</summary>
        public static string Fill(Content c, GameState s, string text, DateTime date) =>
            Offices.Placeholder.Replace(text, m =>
                Holder(c, s, m.Groups[1].Value, date)?.Name
                ?? throw new InvalidOperationException(FormattableString.Invariant($"no holder of '{m.Groups[1].Value}' on {date:yyyy-MM-dd}")));

        /// <summary>Every playable office that exists on the state's date, in offices.yaml order, with its holder (null = vacant).
        /// A player's appointee is the seat's only holder (an appointment from a save made before an office allowed several
        /// holders), so Seat.Holders never disagrees with Seat.Holder.</summary>
        public static IReadOnlyList<Seat> Lineup(Content c, GameState s) =>
            c.Offices.Where(o => !o.HistoricalOnly && o.ExistsOn(s.Date))
                .Select(o =>
                {
                    bool appointed = Active(c, s, o.Id, s.Date) != null;
                    return new Seat(o, Holder(c, s, o.Id), appointed, o.SeveralHolders && !appointed ? Offices.Holders(c, o.Id, s.Date) : null);
                })
                .ToList();

        /// <summary>Null when <paramref name="personId"/> may be appointed to <paramref name="officeId"/> now, else the
        /// reason shown to the player. Checks, in order: the office exists, has not been abolished and has been established; it
        /// is not the Prime Minister's (the player) or the President's (elected by the Knesset); the person exists, is alive, is not
        /// the sitting President, is not the serving Chief of Staff (for any other office) and does not already hold it; a
        /// government is in office; a Chief of Staff must be a career officer (has held the post in people.yaml); a
        /// minister's party, if any, must be in the coalition.</summary>
        /// <remarks>Holding two portfolios is allowed by design: historically Ben-Gurion was Prime Minister and Defence, and Shapira held three.</remarks>
        public static string? Check(Content c, GameState s, string officeId, string personId) => CheckReason(c, s, officeId, personId)?.Text;

        /// <summary>Null when the appointment is legal, else a stable kind and invariant values for a UI, plus English text.</summary>
        public static CabinetReason? CheckReason(Content c, GameState s, string officeId, string personId)
        {
            var office = c.Offices.FirstOrDefault(o => o.Id == officeId);
            if (office == null) return new CabinetReason("office_unknown", "That office does not exist in this game");
            if (office.HistoricalOnly) return new CabinetReason("historical_office", $"{office.Title} is a historical title, not an appointable office", officeId);
            if (office.Until.HasValue && s.Date >= office.Until.Value)
                return new CabinetReason("abolished", $"The office of {office.Title} was abolished on {PlayerText.Date(office.Until.Value)}", officeId,
                    office.Until.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            if (office.From.HasValue && s.Date < office.From.Value)
                return new CabinetReason("not_established", $"The office of {office.Title} is not established until {PlayerText.Date(office.From.Value)}", officeId,
                    office.From.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            if (office.SeveralHolders) return new CabinetReason("several_holders", $"{office.Title} can have several holders at once, and appointing to it is not part of the game yet", officeId);
            if (officeId == "prime_minister") return new CabinetReason("prime_minister", "You are the Prime Minister: that office is not yours to fill");
            if (officeId == "president") return new CabinetReason("president", "The President is elected by the Knesset, not appointed");
            var person = c.People.FirstOrDefault(p => p.Id == personId);
            if (person == null) return new CabinetReason("person_unknown", "That person is not in this game");
            if (person.Died.HasValue && s.Date >= person.Died.Value)
                return new CabinetReason("died", $"{person.Name} died on {PlayerText.Date(person.Died.Value)}", personId,
                    person.Died.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            if (Holder(c, s, "president")?.Id == personId) return new CabinetReason("is_president", $"{person.Name} is the President", personId);
            if (officeId != "chief_of_staff" && Holder(c, s, "chief_of_staff")?.Id == personId)
                return new CabinetReason("is_chief_of_staff", $"{person.Name} is the serving Chief of Staff", personId);
            if (Holder(c, s, officeId)?.Id == personId)
                return new CabinetReason("already_holds", $"{person.Name} already holds the office of {office.Title}", personId, officeId);
            var gov = Offices.Government(c, s.Date);
            if (gov == null) return new CabinetReason("no_government", "No government is in office");
            if (officeId == "chief_of_staff")
                return person.Offices.Any(t => t.Office == "chief_of_staff") ? null
                    : new CabinetReason("not_career_officer", $"{person.Name} is not an IDF career officer", personId);
            var party = Offices.PartyOf(c, person, s.Date);
            if (party != "" && !gov.CoalitionOn(s.Date).Contains(party))
                return new CabinetReason("party_not_coalition", $"{person.Name}'s party ({c.Parties.First(p => p.Id == party).Name}) is not in the coalition", personId, party);
            return null;
        }

        /// <summary>Every person with the reason they cannot be appointed (null = they can), in people.yaml order.</summary>
        public static IReadOnlyList<(Person person, CabinetReason? reason)> Candidates(Content c, GameState s, string officeId) =>
            c.People.Select(p => (p, CheckReason(c, s, officeId, p.Id))).ToList();

        /// <summary>Returns null and appoints, or the reason (state untouched). Replacing a minister costs coalition
        /// stability: rules appoint_stability_cost_other_party when the seat leaves the displaced minister's party (a
        /// non-party appointee counts as another party), appoint_stability_cost_same_party otherwise. A vacant seat costs
        /// nothing.</summary>
        public static string? Appoint(Content c, GameState s, string officeId, string personId, List<Cause> why)
        {
            var reason = Check(c, s, officeId, personId);
            if (reason != null) return reason;
            var gov = Offices.Government(c, s.Date)!;
            var office = c.Offices.First(o => o.Id == officeId);
            var person = c.People.First(p => p.Id == personId);
            var displaced = Holder(c, s, officeId);
            if (s.AppointedUnder != gov.Id) { s.Appointments.Clear(); s.AppointedUnder = gov.Id; }
            s.Appointments[officeId] = new Appointment(personId, s.Date);
            why.Add(new Cause("cabinet", officeId, 0, displaced == null
                ? $"{person.Name} appointed {office.Title}"
                : $"{person.Name} appointed {office.Title}, replacing {displaced.Name}"));
            if (displaced == null) return null;
            var oldParty = Offices.PartyOf(c, displaced, s.Date);
            var newParty = Offices.PartyOf(c, person, s.Date);
            bool sameParty = oldParty != "" && oldParty == newParty;
            var rules = c.RulesAt(s.Date);
            var cost = sameParty ? rules.AppointStabilityCostSameParty : rules.AppointStabilityCostOtherParty;
            if (cost <= 0) return null;
            var before = s.CoalitionStability;
            s.CoalitionStability = Math.Max(0, before - cost);
            var who = oldParty == "" ? displaced.Name : c.Parties.First(p => p.Id == oldParty).Name;
            why.Add(new Cause("politics", "coalition_stability", s.CoalitionStability - before, (
                $"coalition stability {PlayerText.PercentChange(s.CoalitionStability - before)} ({who} {(sameParty ? "resents the reshuffle" : "lost the " + office.Title)})")));
            return null;
        }

        /// <summary>Called after the date advances: when a new government takes office, the player's appointments end.</summary>
        public static void ExpireOnGovernmentChange(Content c, GameState s, List<Cause> why)
        {
            if (s.AppointedUnder == "" || Offices.Government(c, s.Date)?.Id == s.AppointedUnder) return;
            if (s.Appointments.Count > 0)
                why.Add(new Cause("cabinet", "appointments", -s.Appointments.Count, "A new government took office with its own ministers; your appointments ended"));
            s.Appointments.Clear();
            s.AppointedUnder = "";
        }
    }
}
