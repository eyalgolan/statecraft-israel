using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>The civilians of one city who keep a qualification (R-QUAL-001; #1511, owner ruling "Two-way, keep training",
    /// 23 October 2026): craftsmen, halutzim and veteran pioneers who went back to civilian life. Immutable; a change replaces
    /// the record. They are part of <see cref="City.Civilians"/>, so jobs, housing and training treat them as ordinary civilians
    /// (owner ruling 2). A halutz keeps the turns of service it had (owner ruling 3), one entry per group.</summary>
    public sealed class Qualified
    {
        public static readonly Qualified None = new Qualified(0, 0, Array.Empty<int>());

        public int Craftsmen { get; }
        public int Veterans { get; }
        /// <summary>The service of each qualified halutz group, ascending (so the record is the same whatever order they returned in).</summary>
        public IReadOnlyList<int> Halutzim { get; }

        public Qualified(int craftsmen, int veterans, IReadOnlyList<int> halutzim)
        {
            Craftsmen = craftsmen; Veterans = veterans; Halutzim = halutzim.OrderBy(x => x).ToArray();
        }

        public int Total => Craftsmen + Veterans + Halutzim.Count;

        public Qualified WithCraftsmen(int n) => new Qualified(n, Veterans, Halutzim);
        public Qualified WithVeterans(int n) => new Qualified(Craftsmen, n, Halutzim);

        /// <summary>One more halutz group with <paramref name="service"/> turns of service.</summary>
        public Qualified AddHalutz(int service) => new Qualified(Craftsmen, Veterans, Halutzim.Append(service).OrderBy(x => x).ToArray());

        /// <summary>Without the <paramref name="n"/> halutz groups with the most service (R-QUAL-005's pick).</summary>
        public Qualified TakeHalutzim(int n) => new Qualified(Craftsmen, Veterans, Halutzim.Take(Halutzim.Count - n).ToArray());

        /// <summary>Without <paramref name="n"/> qualified people, taken in the order R-QUAL-006 sets: craftsmen, then halutz
        /// groups with the least service, then veterans.</summary>
        public Qualified Lose(int n)
        {
            var c = Math.Min(n, Craftsmen); n -= c;
            var h = Math.Min(n, Halutzim.Count); n -= h;
            return new Qualified(Craftsmen - c, Veterans - n, Halutzim.Skip(h).ToArray());
        }

        /// <summary>The state hash's text: nothing at all when nobody is qualified, so a city without them hashes as before
        /// (R-LOOP-002's rule).</summary>
        public string Canonical()
        {
            var text = "";
            if (Craftsmen != 0) text += ",qcraftsmen=" + Craftsmen.ToString(CultureInfo.InvariantCulture);
            if (Halutzim.Count != 0) text += ",qhalutz=" + string.Join(":", Halutzim.Select(x => x.ToString(CultureInfo.InvariantCulture)));
            if (Veterans != 0) text += ",qveterans=" + Veterans.ToString(CultureInfo.InvariantCulture);
            return text;
        }
    }

    /// <summary>Return to civilian life and reassignment (docs/rules/mvp.md R-QUAL-001 to R-QUAL-007; #1511). Instant, no Money:
    /// a craftsman's or a halutz's qualification is kept in the city, and a qualified civilian without a job goes back into
    /// the trained role without R-TRAIN-001's training.</summary>
    public static class Qualification
    {
        public const string ReturnCraftsmenKind = "return_craftsmen";
        public const string ReassignCraftsmenKind = "reassign_craftsmen";
        public const string ReassignPioneersKind = "reassign_pioneers";
        /// <summary>The unit order of the pioneer's return (R-QUAL-003).</summary>
        public const string ReturnOrder = "return_civilian";

        static string Groups(int n) => n == 1 ? "1 group" : FormattableString.Invariant($"{n} groups");

        /// <summary>R-QUAL-002: <paramref name="count"/> of the city's craftsmen become qualified civilians. Refused, state
        /// untouched, for an unknown city, a count below 1 or more than the city's craftsmen.</summary>
        public static string? ReturnCraftsmen(Content c, GameState s, string city, int count, List<Cause> why)
        {
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            if (count < 1) return "Return at least one group";
            var name = PlayerText.Settlement(c, city);
            if (count > x.Craftsmen) return FormattableString.Invariant($"{name} has {x.Craftsmen} craftsmen");
            s.Cities[city] = x.WithCraftsmen(x.Craftsmen - count).WithCounts(x.Civilians + count, x.Employed, x.Refugees)
                .WithQualified(x.Qualified.WithCraftsmen(x.Qualified.Craftsmen + count));
            why.Add(new Cause("city", city, count, $"{name}: {Groups(count)} of craftsmen returned to civilian life and keep their training"));
            return null;
        }

        /// <summary>R-QUAL-004: <paramref name="count"/> of the city's qualified craftsmen, without a job, become craftsmen
        /// again. Refused, state untouched, for an unknown city, a count below 1, more than the qualified craftsmen, or more than
        /// the civilians without work (<see cref="Training.Idle"/>).</summary>
        public static string? ReassignCraftsmen(Content c, GameState s, string city, int count, List<Cause> why)
        {
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            if (count < 1) return "Reassign at least one group";
            var name = PlayerText.Settlement(c, city);
            if (count > x.Qualified.Craftsmen) return FormattableString.Invariant($"{name} has {x.Qualified.Craftsmen} qualified craftsmen");
            var idle = Training.Idle(x);
            if (count > idle) return FormattableString.Invariant($"{name} has {idle} civilians without work");
            s.Cities[city] = x.WithCounts(x.Civilians - count, x.Employed, x.Refugees).WithCraftsmen(x.Craftsmen + count)
                .WithQualified(x.Qualified.WithCraftsmen(x.Qualified.Craftsmen - count));
            why.Add(new Cause("city", city, -count, $"{name}: {Groups(count)} of qualified craftsmen are craftsmen again"));
            return null;
        }

        /// <summary>R-QUAL-003: an idle halutz or veteran pioneer unit leaves the map and joins the nearest city's civilians,
        /// qualified (its service kept). Null when it takes the order, else the reason; changes nothing, so the unit card can
        /// ask before it offers the order.</summary>
        public static string? CheckReturn(GameState s, int unitId)
        {
            if (!s.Units.TryGetValue(unitId, out var u)) return "There is no such unit";
            if (!Projects.IsPioneer(u.Type)) return "Only a pioneer unit takes that order";
            if (Projects.IsBusy(s, unitId)) return "That unit is at work; stop the work first";
            return null;
        }

        internal static string? Return(Content c, GameState s, int unitId, List<Cause> why)
        {
            var refused = CheckReturn(s, unitId);
            if (refused != null) return refused;
            var u = s.Units[unitId];
            var city = Demobilization.Nearest(c, u.Q, u.R);
            s.Units.Remove(unitId);
            var x = s.Cities[city];
            var q = u.Type == Projects.VeteranType ? x.Qualified.WithVeterans(x.Qualified.Veterans + 1) : x.Qualified.AddHalutz(u.Service);
            s.Cities[city] = x.WithCounts(x.Civilians + 1, x.Employed, x.Refugees).WithQualified(q);
            why.Add(new Cause("map", unitId.ToString(CultureInfo.InvariantCulture), 1,
                $"Unit {unitId} returned to civilian life in {PlayerText.Settlement(c, city)} and keeps its training"));
            return null;
        }

        /// <summary>R-QUAL-005: <paramref name="count"/> of the city's qualified halutz groups (<paramref name="track"/>
        /// `halutz`) or veteran pioneers (`veteran_halutz`), without a job, reappear as units at its settlement hex. Refused,
        /// state untouched, for an unknown city, another track, a count below 1, more than the city's qualified ones, or more
        /// than its civilians without work.</summary>
        public static string? ReassignPioneers(Content c, GameState s, string city, string track, int count, List<Cause> why)
        {
            if (!s.Cities.TryGetValue(city, out var x)) return "There is no such city";
            var veteran = track == Projects.VeteranType;
            if (!veteran && track != "halutz") return "There is no such qualification";
            if (count < 1) return "Reassign at least one group";
            var name = PlayerText.Settlement(c, city);
            var have = veteran ? x.Qualified.Veterans : x.Qualified.Halutzim.Count;
            if (count > have) return FormattableString.Invariant($"{name} has {have} qualified {(veteran ? "veteran pioneers" : "halutzim")}");
            var idle = Training.Idle(x);
            if (count > idle) return FormattableString.Invariant($"{name} has {idle} civilians without work");
            var hex = c.SettlementHexes.First(h => h.Id == city);   // every settlement has one (ValidateMap)
            var q = x.Qualified;
            // R-QUAL-005: the most service first (owner, 24 October 2026: "Accept all as built", https://github.com/eyalgolan/statecraft-israel/issues/1511#issuecomment-6375597377). A veteran comes back with the service
            // that made it one (R-VET-001).
            var served = veteran ? new int[count] : q.Halutzim.Skip(q.Halutzim.Count - count).Reverse().ToArray();
            for (int i = 0; i < count; i++)
            {
                var id = s.NextId++;
                s.Units.Add(id, new Unit(id, track, hex.Q, hex.R, veteran ? c.RulesAt(s.Date).Mvp!.VeteranHalutzService!.Value : served[i]));
            }
            s.Cities[city] = x.WithCounts(x.Civilians - count, x.Employed, x.Refugees)
                .WithQualified(veteran ? q.WithVeterans(q.Veterans - count) : q.TakeHalutzim(count));
            why.Add(new Cause("city", city, -count, $"{name}: {Groups(count)} of qualified {(veteran ? "veteran pioneers" : "halutzim")} are units again"));
            return null;
        }
    }
}
