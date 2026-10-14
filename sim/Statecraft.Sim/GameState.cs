using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Statecraft.Sim
{
    /// <summary>The stable codes <see cref="GameState.GameOver"/> can hold (they are in the state hash).</summary>
    public static class GameOverCodes
    {
        /// <summary>The last era's end was reached (eras.yaml).</summary>
        public const string EraEnded = "era_ended";
        public const string CoalitionCollapse = "coalition_collapse";
    }

    public enum Mode { Historical, Open }

    /// <summary>All mutable game data. Systems never share references between states: Step works on Clone().</summary>
    public sealed class GameState
    {
        public DateTime Date;
        public ulong RngState;
        public Mode Mode;
        public int Turn;
        public Dictionary<string, double> Indicators = new Dictionary<string, double>();
        public Dictionary<string, double> Policies = new Dictionary<string, double>();
        public Dictionary<string, double> EffectContributions = new Dictionary<string, double>();
        /// <summary>Permanent per-indicator offsets from event choices, added on top of base and effects.</summary>
        public Dictionary<string, double> IndicatorShifts = new Dictionary<string, double>();
        public SortedSet<string> ActiveSituations = new SortedSet<string>(StringComparer.Ordinal);
        public Dictionary<string, double> Budget = new Dictionary<string, double>();
        public double Treasury;
        public double Debt;
        public double FxReserves;
        public Dictionary<string, double> Population = new Dictionary<string, double>();
        public int Rung = 1;
        public int CeasefireWeeksLeft;
        public double Mobilization;
        public double CoalitionStability = 1.0;
        public double Attention;
        public SortedSet<string> FiredEvents = new SortedSet<string>(StringComparer.Ordinal);
        public List<string> PendingEvents = new List<string>();
        /// <summary>The option each resolved event was resolved with (chosen or delegated), for events' `requires`.</summary>
        public Dictionary<string, string> ChosenOptions = new Dictionary<string, string>();
        /// <summary>Player appointments by office id; valid while the government in AppointedUnder is in office (Cabinet).</summary>
        public Dictionary<string, Appointment> Appointments = new Dictionary<string, Appointment>();
        public string AppointedUnder = "";
        /// <summary>Map actions built so far, in order (MapActions).</summary>
        public List<Placement> Placements = new List<Placement>();
        /// <summary>The turn (its date) the map-action counter belongs to, and the actions accepted in it. Counted on
        /// acceptance, so replacing a camp with housing cannot reopen the per-turn cap; a new date starts from zero.</summary>
        public DateTime MapActionsTurnDate;
        public int MapActionsThisTurn;
        /// <summary>Why the game ended, as one of the <see cref="GameOverCodes"/>; null while it is on. Never display it: use PlayerText.GameOver.</summary>
        public string? GameOver;
        // GDD v0.1 state (spec 2026-10-12 section 2.2). Empty in the legacy loop, and then left out of the hash (R-LOOP-002).
        /// <summary>The cities by id (ordinal).</summary>
        public SortedDictionary<string, City> Cities = new SortedDictionary<string, City>(StringComparer.Ordinal);
        /// <summary>Social cohesion, 0 to 1, from the cities' housing and jobs (R-CITY-005). A placeholder while D-C1 is open:
        /// no other system reads it and nothing shows it. Hashed only when the state has cities.</summary>
        public double Cohesion;
        /// <summary>The movable units by id.</summary>
        public SortedList<int, Unit> Units = new SortedList<int, Unit>();
        /// <summary>The groups in training by id (R-TRAIN-001). A cohort leaves when it finishes or is cancelled.</summary>
        public SortedList<int, Cohort> Cohorts = new SortedList<int, Cohort>();
        /// <summary>The designated sites being built or built, by site id (ordinal; R-SITE-001). An available site has no record.</summary>
        public SortedDictionary<string, SiteWork> Sites = new SortedDictionary<string, SiteWork>(StringComparer.Ordinal);
        /// <summary>The next id a new unit or training cohort takes (they share it). Ids are never reused.</summary>
        public int NextId;
        /// <summary>The date each completed objective was completed, by objective id (ordinal).</summary>
        public SortedDictionary<string, DateTime> Objectives = new SortedDictionary<string, DateTime>(StringComparer.Ordinal);
        /// <summary>The player's score (GDD "Objectives": bonus points). An int assumes whole-number points; the point values and
        /// the timing bonus are open decisions with the owner (D-B2, D-B6), so the type is revisited when they are set.</summary>
        public int Score;

        public double TotalPopulation => Population.Values.Sum();

        public GameState Clone()
        {
            var c = (GameState)MemberwiseClone();
            c.Indicators = new Dictionary<string, double>(Indicators);
            c.Policies = new Dictionary<string, double>(Policies);
            c.EffectContributions = new Dictionary<string, double>(EffectContributions);
            c.IndicatorShifts = new Dictionary<string, double>(IndicatorShifts);
            c.ActiveSituations = new SortedSet<string>(ActiveSituations, StringComparer.Ordinal);
            c.Budget = new Dictionary<string, double>(Budget);
            c.Population = new Dictionary<string, double>(Population);
            c.FiredEvents = new SortedSet<string>(FiredEvents, StringComparer.Ordinal);
            c.PendingEvents = new List<string>(PendingEvents);
            c.ChosenOptions = new Dictionary<string, string>(ChosenOptions);
            c.Appointments = new Dictionary<string, Appointment>(Appointments);
            c.Placements = new List<Placement>(Placements);
            c.Cities = new SortedDictionary<string, City>(Cities, StringComparer.Ordinal);
            c.Units = new SortedList<int, Unit>(Units);
            c.Cohorts = new SortedList<int, Cohort>(Cohorts);
            c.Sites = new SortedDictionary<string, SiteWork>(Sites, StringComparer.Ordinal);
            c.Objectives = new SortedDictionary<string, DateTime>(Objectives, StringComparer.Ordinal);
            return c;
        }

        /// <summary>SHA-256 over a canonical text form (ordinal-sorted keys, doubles as IEEE bit patterns with -0.0
        /// written as 0.0), so hashes match across .NET, Mono and IL2CPP.</summary>
        public string Hash()
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(CanonicalText(false)));
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }

        /// <summary>The state in the order and form <see cref="Hash"/> hashes it, one line per section, with each number
        /// also written as a plain value (`name=1.5 [3FF8000000000000]`). For humans: a golden-hash failure diffs this
        /// (issue #256). It shares one writer with <see cref="Hash"/>, so the two cannot list different fields.</summary>
        public string Describe() => CanonicalText(true);

        string CanonicalText(bool readable)
        {
            // Hash text: one line, ';'-separated, closing brackets. Readable text: a section label per line, each entry
            // on its own indented line, no closing brackets. Both are written by the calls below, in the same order.
            var sb = new StringBuilder();
            void Nl() { if (readable) sb.Append('\n'); }
            void Entry() { if (readable) sb.Append("\n  "); }
            void Close(char c) { if (!readable) sb.Append(c); }
            void Num(string k, double v, bool inMap = false)
            {
                var bits = BitConverter.DoubleToInt64Bits(v == 0.0 ? 0.0 : v).ToString("X16", CultureInfo.InvariantCulture);
                if (readable) sb.Append(inMap ? "\n  " : "\n").Append(k).Append('=').Append((v == 0.0 ? 0.0 : v).ToString("R", CultureInfo.InvariantCulture)).Append(" [").Append(bits).Append(']');
                else sb.Append(k).Append('=').Append(bits).Append(';');
            }
            void Map(string name, Dictionary<string, double> m)
            {
                Nl(); sb.Append(name).Append('{');
                foreach (var kv in m.OrderBy(k => k.Key, StringComparer.Ordinal)) Num(kv.Key, kv.Value, true);
                Close('}');
            }
            void List(string name, IEnumerable<string> items)
            {
                Nl(); sb.Append(name).Append('[');
                if (readable) foreach (var i in items) { Entry(); sb.Append(i); }
                else sb.Append(string.Join(",", items));
                Close(']');
            }
            void Sep() { if (!readable) sb.Append(';'); }
            sb.Append(Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(';')
              .Append(RngState).Append(';').Append(Mode).Append(';').Append(Turn).Append(';');
            Map("ind", Indicators); Map("pol", Policies); Map("eff", EffectContributions); Map("shift", IndicatorShifts);
            Map("bud", Budget); Map("pop", Population);
            List("sit", ActiveSituations);
            Num("treasury", Treasury); Num("debt", Debt); Num("fx", FxReserves);
            Nl(); sb.Append("rung=").Append(Rung).Append(";cf=").Append(CeasefireWeeksLeft).Append(';');
            Num("mob", Mobilization); Num("stab", CoalitionStability); Num("att", Attention);
            List("fired", FiredEvents);
            List("pending", PendingEvents);
            Nl(); sb.Append("chosen{");
            foreach (var kv in ChosenOptions.OrderBy(k => k.Key, StringComparer.Ordinal)) { Entry(); sb.Append(kv.Key).Append('=').Append(kv.Value); Sep(); }
            Close('}');
            Nl(); sb.Append("appt{");
            foreach (var kv in Appointments.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                Entry(); sb.Append(kv.Key).Append('=').Append(kv.Value.Person).Append('@').Append(kv.Value.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); Sep();
            }
            Close('}');
            Nl(); sb.Append("under=").Append(AppointedUnder).Append(';');
            Nl(); sb.Append("map[");
            foreach (var p in Placements)
            {
                Entry(); sb.Append(p.Action).Append('@').Append(p.Q).Append(',').Append(p.R).Append('@').Append(p.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); Sep();
            }
            Close(']');
            Nl(); sb.Append("mapturn=").Append(MapActionsTurnDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(':').Append(MapActionsThisTurn).Append(';');
            Nl(); sb.Append("over=").Append(GameOver ?? "");
            // GDD v0.1 sections (spec 2026-10-12 section 2.4): each written only when set, so with the legacy loop the text
            // above is the whole text, byte for byte. The over= line has no separator after it, so each section adds one.
            // R-LOOP-002.
            void Section(string name, char open, char close, IEnumerable<string> items)
            {
                Sep(); Nl(); sb.Append(name).Append(open);
                foreach (var i in items) { Entry(); sb.Append(i); Sep(); }
                Close(close);
            }
            if (Cities.Count > 0)
            {
                Section("city", '{', '}', Cities.Values.Select(x => x.Canonical()));
                Sep();
                var bits = BitConverter.DoubleToInt64Bits(Cohesion == 0.0 ? 0.0 : Cohesion).ToString("X16", CultureInfo.InvariantCulture);
                Nl(); sb.Append("coh=");
                if (readable) sb.Append((Cohesion == 0.0 ? 0.0 : Cohesion).ToString("R", CultureInfo.InvariantCulture)).Append(" [").Append(bits).Append(']');
                else sb.Append(bits);
            }
            if (Units.Count > 0) Section("units", '[', ']', Units.Values.Select(x => x.Canonical()));
            if (Cohorts.Count > 0) Section("train", '[', ']', Cohorts.Values.Select(x => x.Canonical()));
            if (Sites.Count > 0) Section("site", '{', '}', Sites.Values.Select(x => x.Canonical()));
            if (NextId != 0) { Sep(); Nl(); sb.Append("next=").Append(NextId.ToString(CultureInfo.InvariantCulture)); }
            if (Objectives.Count > 0)
                Section("obj", '{', '}', Objectives.Select(kv => kv.Key + "=" + kv.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            if (Score != 0) { Sep(); Nl(); sb.Append("score=").Append(Score.ToString(CultureInfo.InvariantCulture)); }
            return sb.ToString();
        }
    }
}
