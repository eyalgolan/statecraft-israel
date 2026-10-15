using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace Statecraft.Sim
{
    // Plain data classes filled by YamlDotNet (UnderscoredNamingConvention: SeatsByParty <- seats_by_party).

    public sealed class Source { public string Url { get; set; } = ""; public DateTime Checked { get; set; } }

    /// <summary>The shape of a sign-off's `reviewed_by`: a GitHub handle (letters and digits, single hyphens between them,
    /// at most 39 characters, and at least one letter, so a bare number such as 1234 reads the same in every reader), so never an email, an @-name, a display name or a bot account ("name[bot]"). Shared with
    /// the map's readers in Unity.</summary>
    public static class ReviewRecord
    {
        static readonly System.Text.RegularExpressions.Regex Handle =
            new System.Text.RegularExpressions.Regex(@"^(?=[A-Za-z0-9-]*[A-Za-z])[A-Za-z0-9](?:[A-Za-z0-9]|-(?=[A-Za-z0-9])){0,38}\z");

        public static bool IsHandle(string? s) => s != null && Handle.IsMatch(s);

        static readonly System.Text.RegularExpressions.Regex Day = new System.Text.RegularExpressions.Regex(@"^\d{4}-\d{2}-\d{2}\z");

        /// <summary>Exactly YYYY-MM-DD, and a real day (no 30 February).</summary>
        public static bool IsDay(string? s) => s != null && Day.IsMatch(s)
            && DateTime.TryParseExact(s, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _);

        /// <summary>What is wrong with a row's sign-off, as (field, message), or null when it is absent or whole: both keys or
        /// neither (a key that is present but blank counts as present), the handle's shape, and the day as YYYY-MM-DD.</summary>
        public static (string field, string message)? Problem(bool byGiven, string? by, bool onGiven, string? on)
        {
            if (!byGiven && !onGiven) return null;
            if (!onGiven) return ("reviewed_on", "required with reviewed_by: the day the person signed off (YYYY-MM-DD)");
            if (!byGiven) return ("reviewed_by", "required with reviewed_on: the reviewer's GitHub handle");
            if (!IsHandle(by)) return ("reviewed_by", "must be the reviewer's GitHub handle (letters, digits and single hyphens; no @, email or display name)");
            if (!IsDay(on))
                return ("reviewed_on", "must be the day of the sign-off, YYYY-MM-DD");
            return null;
        }
    }

    /// <summary>The review flag of a Hebrew display name (`*_he_review`, CONTENT_POLICY section 2, rule 6). A name an AI agent
    /// checked against its sources, but no person has read yet, carries <see cref="AgentChecked"/>; the row's own `status`
    /// stays what it is for the English facts. A native speaker's review clears the flag (the key is removed).</summary>
    public static class HebrewReview
    {
        public const string AgentChecked = "agent-checked";
    }

    public sealed class Office
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        /// <summary>The Hebrew title (#558), sourced in <see cref="TitleHeSources"/>; empty when no source gives one (the UI then
        /// keeps the English).</summary>
        public string TitleHe { get; set; } = "";
        public List<Source> TitleHeSources { get; set; } = new List<Source>();
        /// <summary>"agent-checked" while <see cref="TitleHe"/> waits for a native speaker's review (#564); empty once a
        /// person has reviewed it (<see cref="HebrewReview"/>).</summary>
        public string TitleHeReview { get => titleHeReview ?? ""; set => titleHeReview = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank flag is an error, not a review.</summary>
        [YamlIgnore] public bool TitleHeReviewGiven => titleHeReview != null;
        string? titleHeReview;
        /// <summary>The feminine Hebrew title, only where a woman held the office (the Knesset records write שרת העבודה);
        /// sourced in <see cref="TitleHeFSources"/>. Needs <see cref="TitleHe"/>.</summary>
        public string TitleHeF { get; set; } = "";
        public List<Source> TitleHeFSources { get; set; } = new List<Source>();
        /// <summary>As <see cref="TitleHeReview"/>, for <see cref="TitleHeF"/>.</summary>
        public string TitleHeFReview { get => titleHeFReview ?? ""; set => titleHeFReview = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank flag is an error, not a review.</summary>
        [YamlIgnore] public bool TitleHeFReviewGiven => titleHeFReview != null;
        string? titleHeFReview;
        /// <summary>Historical record only: no Cabinet seat, player appointment or gameplay responsibility. Defaults false for existing offices.</summary>
        public bool HistoricalOnly { get; set; }
        /// <summary>True when several people may hold the office on the same day (Minister without Portfolio, a second
        /// deputy minister). Every other office has one holder at a time, and the loader refuses overlapping terms.</summary>
        public bool SeveralHolders { get; set; }
        /// <summary>Day the office was established (inclusive, like a term's `from`); null when it exists from the start.</summary>
        public DateTime? From { get; set; }
        /// <summary>Day the office was abolished (exclusive end, like a term's `to`); null while it still exists.</summary>
        public DateTime? Until { get; set; }
        /// <summary>Id of the office that takes over when this one is abolished (chapters spec 3.2). Empty means none.
        /// Read and validated by the loader; used when an era boundary is crossed (#59).</summary>
        public string Successor { get; set; } = "";
        /// <summary>True when the office exists on <paramref name="date"/>: on or after From and before Until.</summary>
        public bool ExistsOn(DateTime date) => (!From.HasValue || date >= From.Value) && (!Until.HasValue || date < Until.Value);
    }

    /// <summary>A chapter of the game: the half-open range [From, To). Eras are contiguous; the last era's To ends the game.</summary>
    public sealed class Era
    {
        public string Id { get; set; } = "";
        /// <summary>The chapter name the player sees.</summary>
        public string Name { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        /// <summary>The era's system switches (eras.yaml `systems`). Absent means the defaults: a state legislature and every map action.</summary>
        public EraSystems Systems { get; set; } = new EraSystems();
    }

    /// <summary>Switches for code that exists (chapters design §5). A key is added here only with the code behind it.</summary>
    public sealed class EraSystems
    {
        public const string State = "state";
        public const string Assembly = "assembly";
        /// <summary>`state`: the sitting body's votes bind. `assembly`: a consent body without force of law; its votes are
        /// held and reported, but a vote against does not stop a policy or a budget.</summary>
        public string Legislature { get; set; } = State;
        /// <summary>The map actions allowed in the era (transit_camp, housing). Absent means both. An empty list means none.</summary>
        public List<string> MapActions { get; set; } = new List<string> { "transit_camp", "housing" };
        public const string Legacy = "legacy";
        public const string Gdd = "gdd";
        /// <summary>`legacy`: today's game. `gdd`: the GDD v0.1 loop (docs/GDD.md), built behind this switch until cutover
        /// (spec 2026-10-12 section 2.1). One value for the whole game: the loader refuses eras that differ, because a save
        /// names one ruleset (section 2.4). R-LOOP-001.</summary>
        public string Loop { get; set; } = Legacy;
    }

    public sealed class Party
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        /// <summary>The Hebrew name (#558), sourced in <see cref="NameHeSources"/>; empty when no source gives one.</summary>
        public string NameHe { get; set; } = "";
        public List<Source> NameHeSources { get; set; } = new List<Source>();
        /// <summary>"agent-checked" while <see cref="NameHe"/> waits for a native speaker's review (#564); empty once reviewed.</summary>
        public string NameHeReview { get => nameHeReview ?? ""; set => nameHeReview = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank flag is an error, not a review.</summary>
        [YamlIgnore] public bool NameHeReviewGiven => nameHeReview != null;
        string? nameHeReview;
    }

    public sealed class OfficeTerm
    {
        public string Office { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        /// <summary>Party the holder represented in this term; empty means Person.Party applies.</summary>
        public string Party { get; set; } = "";
        public List<Source> Sources { get; set; } = new List<Source>();
    }

    public sealed class Portrait
    {
        public string File { get; set; } = ""; public string License { get; set; } = ""; public string Source { get; set; } = "";
        /// <summary>Where the face is: its centre's height as a share of the picture (0 = top, 1 = bottom), so the event card
        /// can keep it in view (playtest 2, P2-5). Required for a portrait with a file.</summary>
        public double? FocusY { get; set; }
    }

    public sealed class Person
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NameHe { get; set; } = "";
        /// <summary>Biographical birth date; optional until the sourced person data is complete.</summary>
        public DateTime? Born { get; set; }
        /// <summary>Sources for each biography value (name_he, born, died), separate from dated office-term sources.</summary>
        public Dictionary<string, List<Source>> BiographySources { get; set; } = new Dictionary<string, List<Source>>();
        /// <summary>Required (null only when missing, which is a load error): a living person is excluded from release builds.</summary>
        public bool? Living { get; set; }
        /// <summary>Date of death, when it falls inside the content range: every term must end by then.</summary>
        public DateTime? Died { get; set; }
        public int Competence { get; set; }
        public int Loyalty { get; set; }
        public string Party { get; set; } = "";
        public Portrait? Portrait { get; set; }
        public List<OfficeTerm> Offices { get; set; } = new List<OfficeTerm>();
        public string Status { get; set; } = "draft";
        /// <summary>The person who signed this row off: a GitHub handle, written only by that person (owner, 15 October 2026,
        /// #375: "Yes, humans only"; scripts/oss/reviewed_by_guard.py). Empty when no one has; with <see cref="ReviewedOn"/>.</summary>
        public string ReviewedBy { get => reviewedBy ?? ""; set => reviewedBy = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank sign-off is an error.</summary>
        [YamlIgnore] public bool ReviewedByGiven => reviewedBy != null;
        string? reviewedBy;
        /// <summary>The day of the sign-off, YYYY-MM-DD, as the file writes it (read as text, so the loader checks the same
        /// shape as tools/review_record.py and the map's readers; YamlDotNet's own date parsing would take "15 October 2026");
        /// set exactly when <see cref="ReviewedBy"/> is.</summary>
        public string ReviewedOn { get => reviewedOn ?? ""; set { reviewedOn = value ?? ""; ReviewedOnGiven = true; } }
        /// <summary>True when the file has the key at all, even blank.</summary>
        [YamlIgnore] public bool ReviewedOnGiven { get; private set; }
        string? reviewedOn;
    }

    public sealed class Legislature
    {
        public string Id { get; set; } = "";
        public string Body { get; set; } = "";
        /// <summary>The Hebrew name of the body (#558), sourced in <see cref="BodyHeSources"/>; empty when no source gives one.</summary>
        public string BodyHe { get; set; } = "";
        public List<Source> BodyHeSources { get; set; } = new List<Source>();
        /// <summary>"agent-checked" while <see cref="BodyHe"/> waits for a native speaker's review (#564); empty once reviewed.</summary>
        public string BodyHeReview { get => bodyHeReview ?? ""; set => bodyHeReview = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank flag is an error, not a review.</summary>
        [YamlIgnore] public bool BodyHeReviewGiven => bodyHeReview != null;
        string? bodyHeReview;
        public DateTime? Elected { get; set; }
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public int Size { get; set; }
        public bool BudgetApproval { get; set; }
        public Dictionary<string, int> Seats { get; set; } = new Dictionary<string, int>();
        /// <summary>Fields of this record not yet verified (spec section 5 "open fact"). Kept explicit and tested.</summary>
        public List<string> OpenQuestions { get; set; } = new List<string>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
        /// <summary>The person who signed this row off: a GitHub handle, written only by that person (owner, 15 October 2026,
        /// #375: "Yes, humans only"; scripts/oss/reviewed_by_guard.py). Empty when no one has; with <see cref="ReviewedOn"/>.</summary>
        public string ReviewedBy { get => reviewedBy ?? ""; set => reviewedBy = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank sign-off is an error.</summary>
        [YamlIgnore] public bool ReviewedByGiven => reviewedBy != null;
        string? reviewedBy;
        /// <summary>The day of the sign-off, YYYY-MM-DD, as the file writes it (read as text, so the loader checks the same
        /// shape as tools/review_record.py and the map's readers; YamlDotNet's own date parsing would take "15 October 2026");
        /// set exactly when <see cref="ReviewedBy"/> is.</summary>
        public string ReviewedOn { get => reviewedOn ?? ""; set { reviewedOn = value ?? ""; ReviewedOnGiven = true; } }
        /// <summary>True when the file has the key at all, even blank.</summary>
        [YamlIgnore] public bool ReviewedOnGiven { get; private set; }
        string? reviewedOn;
    }

    public sealed class Government
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        /// <summary>The Hebrew name (#558), sourced in <see cref="NameHeSources"/>; empty when no source gives one.</summary>
        public string NameHe { get; set; } = "";
        public List<Source> NameHeSources { get; set; } = new List<Source>();
        /// <summary>"agent-checked" while <see cref="NameHe"/> waits for a native speaker's review (#564); empty once reviewed.</summary>
        public string NameHeReview { get => nameHeReview ?? ""; set => nameHeReview = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank flag is an error, not a review.</summary>
        [YamlIgnore] public bool NameHeReviewGiven => nameHeReview != null;
        string? nameHeReview;
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        /// <summary>The parties in the coalition when the government took office. Parties that join or leave later are
        /// in <see cref="CoalitionChanges"/>; read the coalition of a given day with <see cref="CoalitionOn"/>.</summary>
        public List<string> Coalition { get; set; } = new List<string>();
        /// <summary>Parties joining or leaving the coalition inside the term, in date order (the loader checks it).</summary>
        public List<CoalitionChange> CoalitionChanges { get; set; } = new List<CoalitionChange>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
        /// <summary>The person who signed this row off: a GitHub handle, written only by that person (owner, 15 October 2026,
        /// #375: "Yes, humans only"; scripts/oss/reviewed_by_guard.py). Empty when no one has; with <see cref="ReviewedOn"/>.</summary>
        public string ReviewedBy { get => reviewedBy ?? ""; set => reviewedBy = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank sign-off is an error.</summary>
        [YamlIgnore] public bool ReviewedByGiven => reviewedBy != null;
        string? reviewedBy;
        /// <summary>The day of the sign-off, YYYY-MM-DD, as the file writes it (read as text, so the loader checks the same
        /// shape as tools/review_record.py and the map's readers; YamlDotNet's own date parsing would take "15 October 2026");
        /// set exactly when <see cref="ReviewedBy"/> is.</summary>
        public string ReviewedOn { get => reviewedOn ?? ""; set { reviewedOn = value ?? ""; ReviewedOnGiven = true; } }
        /// <summary>True when the file has the key at all, even blank.</summary>
        [YamlIgnore] public bool ReviewedOnGiven { get; private set; }
        string? reviewedOn;

        /// <summary>The coalition on <paramref name="date"/>: the opening list with every change dated on or before that
        /// day applied. The result keeps the opening order, with parties that joined added at the end.</summary>
        public List<string> CoalitionOn(DateTime date)
        {
            var result = new List<string>(Coalition);
            foreach (var change in CoalitionChanges)
            {
                if (change.Date > date) break;
                result.RemoveAll(p => change.Leaves.Contains(p));
                foreach (var p in change.Joins) if (!result.Contains(p)) result.Add(p);
            }
            return result;
        }
    }

    /// <summary>Parties joining or leaving a government's coalition on one day, inside its term. Applies from that day.</summary>
    public sealed class CoalitionChange
    {
        public DateTime Date { get; set; }
        public List<string> Joins { get; set; } = new List<string>();
        public List<string> Leaves { get; set; } = new List<string>();
        public List<Source> Sources { get; set; } = new List<Source>();
    }

    public sealed class Indicator
    {
        public string Id { get; set; } = "";
        public double Base { get; set; }
        /// <summary>Display name shown to the player (Cause text); required.</summary>
        public string Name { get; set; } = "";
        /// <summary>First day the record exists (inclusive); null when it exists from the start (chapters design 3.2).</summary>
        public DateTime? From { get; set; }
        /// <summary>Day the record stops existing (exclusive); null while it still exists.</summary>
        public DateTime? To { get; set; }
        public bool ActiveOn(DateTime date) => Window.Active(From, To, date);

    }

    public sealed class Policy
    {
        public string Id { get; set; } = "";
        public double Default { get; set; }
        public string Office { get; set; } = "";
        /// <summary>Display name shown to the player (Cause text); required.</summary>
        public string Name { get; set; } = "";
        /// <summary>First day the record exists (inclusive); null when it exists from the start (chapters design 3.2).</summary>
        public DateTime? From { get; set; }
        /// <summary>Day the record stops existing (exclusive); null while it still exists.</summary>
        public DateTime? To { get; set; }
        public bool ActiveOn(DateTime date) => Window.Active(From, To, date);

    }

    /// <summary>Additive effect: its contribution to Target moves toward K * source value over Inertia turns.</summary>
    public sealed class Effect
    {
        public string Id { get; set; } = "";
        public string Source { get; set; } = "";
        public string Target { get; set; } = "";
        public double K { get; set; }
        public int Inertia { get; set; } = 1;
        public string Office { get; set; } = "";
        /// <summary>Short display name shown to the player (Cause text); required.</summary>
        public string Name { get; set; } = "";
        /// <summary>First day the record exists (inclusive); null when it exists from the start (chapters design 3.2).</summary>
        public DateTime? From { get; set; }
        /// <summary>Day the record stops existing (exclusive); null while it still exists.</summary>
        public DateTime? To { get; set; }
        public bool ActiveOn(DateTime date) => Window.Active(From, To, date);

    }

    /// <summary>Turns on past one threshold and off past another (hysteresis), so it does not flicker.</summary>
    public sealed class Situation
    {
        public string Id { get; set; } = "";
        public string Indicator { get; set; } = "";
        public double? OnBelow { get; set; }
        public double? OffAbove { get; set; }
        public double? OnAbove { get; set; }
        public double? OffBelow { get; set; }
        /// <summary>Display name shown to the player (Cause text); required.</summary>
        public string Name { get; set; } = "";
        /// <summary>First day the record exists (inclusive); null when it exists from the start (chapters design 3.2).</summary>
        public DateTime? From { get; set; }
        /// <summary>Day the record stops existing (exclusive); null while it still exists.</summary>
        public DateTime? To { get; set; }
        public bool ActiveOn(DateTime date) => Window.Active(From, To, date);

    }

    public sealed class VoterGroup
    {
        public string Id { get; set; } = ""; public Dictionary<string, double> Weights { get; set; } = new Dictionary<string, double>();
        /// <summary>First day the record exists (inclusive); null when it exists from the start (chapters design 3.2).</summary>
        public DateTime? From { get; set; }
        /// <summary>Day the record stops existing (exclusive); null while it still exists.</summary>
        public DateTime? To { get; set; }
        public bool ActiveOn(DateTime date) => Window.Active(From, To, date);
    }

    public sealed class Actor
    {
        public string Id { get; set; } = ""; public string Name { get; set; } = ""; public double Standing { get; set; }
        /// <summary>First day the record exists (inclusive); null when it exists from the start (chapters design 3.2).</summary>
        public DateTime? From { get; set; }
        /// <summary>Day the record stops existing (exclusive); null while it still exists.</summary>
        public DateTime? To { get; set; }
        public bool ActiveOn(DateTime date) => Window.Active(From, To, date);
    }

    /// <summary>Half-open windows [from, to) with null for "no bound" (chapters design 3.2).</summary>
    public static class Window
    {
        public static bool Active(DateTime? from, DateTime? to, DateTime date) => (!from.HasValue || date >= from.Value) && (!to.HasValue || date < to.Value);

        /// <summary>True when a record with window [rFrom, rTo) exists on every day of [from, to): the window of whatever uses it.</summary>
        public static bool Covers(DateTime? rFrom, DateTime? rTo, DateTime? from, DateTime? to) =>
            (!rFrom.HasValue || (from.HasValue && rFrom.Value <= from.Value)) && (!rTo.HasValue || (to.HasValue && rTo.Value >= to.Value));
    }

    /// <summary>The model records that exist on one day (<see cref="Content.At"/>). Built once at load, never changed.</summary>
    public sealed class ModelView
    {
        public IReadOnlyList<Indicator> Indicators { get; }
        public IReadOnlyList<Policy> Policies { get; }
        public IReadOnlyList<Effect> Effects { get; }
        public IReadOnlyList<Situation> Situations { get; }
        public IReadOnlyList<VoterGroup> VoterGroups { get; }
        public IReadOnlyList<Actor> Actors { get; }
        public ModelView(IReadOnlyList<Indicator> i, IReadOnlyList<Policy> p, IReadOnlyList<Effect> e, IReadOnlyList<Situation> s, IReadOnlyList<VoterGroup> v, IReadOnlyList<Actor> a)
        { Indicators = i; Policies = p; Effects = e; Situations = s; VoterGroups = v; Actors = a; }
    }

    public sealed class Rules
    {
        public DateTime Start { get; set; }
        public int StartRung { get; set; } = 1;
        public double AttentionPerTurn { get; set; }
        public Dictionary<string, double> AttentionCost { get; set; } = new Dictionary<string, double>();
        public double RungAttentionStep { get; set; }
        public int CeasefireWeeks { get; set; }
        /// <summary>The turn length on each security rung, rungs 1 to 5: quarter, month or week. The default is the legacy
        /// table; the GDD v0.1 cutover sets all five to month (spec 2026-10-12 section 3). Read through
        /// SecuritySystem.TurnLengthFor(c, date, rung). R-TURN-001.</summary>
        public List<string> TurnLengths { get; set; } = new List<string> { "quarter", "quarter", "quarter", "month", "week" };
        public List<double> RungMinMobilization { get; set; } = new List<double>();
        public double TreasuryStart { get; set; }
        public double FxStart { get; set; }
        public double TaxPerCapitaPerYear { get; set; }
        public double SpendingPerCapitaPerYear { get; set; }
        public double ExportsPerYear { get; set; }
        public double ImportsPerCapitaPerYear { get; set; }
        /// <summary>Foreign currency arriving each year without exports (donations, loans, transfers), in the units of fx_start.</summary>
        public double CapitalInflowPerYear { get; set; }
        /// <summary>Change to food_supply per year while the foreign currency crisis lasts (imports of food are cut); &lt;= 0.</summary>
        public double FxCrisisFoodPerYear { get; set; }
        public double MobilizationCostPerYear { get; set; }
        /// <summary>Coalition stability lost when an appointment takes a seat from the displaced minister's party.</summary>
        public double AppointStabilityCostOtherParty { get; set; }
        /// <summary>Coalition stability lost when an appointment replaces a minister with one of the same party.</summary>
        public double AppointStabilityCostSameParty { get; set; }
        /// <summary>First day map actions can be placed.</summary>
        public DateTime MapActionsFrom { get; set; }
        /// <summary>How far (in hexes) a map action may be from a hex that owns a settlement.</summary>
        public int MapActionMaxDistance { get; set; } = 1;
        /// <summary>How many map actions may be built in one turn.</summary>
        public int MapActionsPerTurn { get; set; } = 2;
        public Dictionary<string, MapActionRule> MapActions { get; set; } = new Dictionary<string, MapActionRule>();
        public double NaturalIncreasePerYear { get; set; }
        public Dictionary<string, double> InitialPopulation { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> BudgetStart { get; set; } = new Dictionary<string, double>();
        /// <summary>Who owns a budget line, by date (#108). A line with no entry here is owned, for the whole era, by the
        /// office with the line's own id. A listed line is owned by each term's office from that term's day to the next
        /// term's day. Read through <see cref="Offices.BudgetOwner"/> and <see cref="Offices.BudgetLineOf"/>. Start-only, like budget_start.</summary>
        public Dictionary<string, List<BudgetOwnerTerm>> BudgetOwners { get; set; } = new Dictionary<string, List<BudgetOwnerTerm>>();
        /// <summary>The GDD v0.1 rules (docs/rules/mvp.md). Required, and read, only when eras.yaml systems.loop is gdd; null
        /// otherwise. Its values are the owner's to set: until then they live in MvpSlice.Overlay or a test fixture.</summary>
        public MvpRules? Mvp { get; set; }
    }

    /// <summary>rules.yaml mvp: the GDD v0.1 slice's city rules (Phase 1 PR 3).</summary>
    public sealed class MvpRules
    {
        /// <summary>People in one population unit (D-A1: a unit is a group).</summary>
        public int PeoplePerUnit { get; set; }
        /// <summary>Money each employed unit earns a year, in IL millions (R-CITY-002).</summary>
        public double IncomePerJobPerYear { get; set; }
        /// <summary>Social cohesion's weight on the housed share (R-CITY-005). A placeholder while D-C1 is open.</summary>
        public double CohesionHousingWeight { get; set; }
        /// <summary>Social cohesion's weight on the employed share (R-CITY-005). A placeholder while D-C1 is open.</summary>
        public double CohesionJobsWeight { get; set; }
        /// <summary>The city every arrival lands in (Phase 1 has one city; R-CITY-003).</summary>
        public string ArrivalsCity { get; set; } = "";
        /// <summary>Turns a "build yishuv" takes, counting the turn it was ordered in (R-SITE-003).</summary>
        public int BuildYishuvTurns { get; set; }
        /// <summary>IL millions a "build yishuv" costs, paid in full when it starts (R-SITE-001).</summary>
        public double BuildYishuvCost { get; set; }
    }

    /// <summary>A designated site (sites.yaml; docs/GDD.md "Pioneer work": building is allowed only at designated spots, and a
    /// new yishuv takes its site's name). Sourced facts only: the balance values of building are rules (MvpRules).</summary>
    public sealed class Site
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NameHe { get; set; } = "";
        public int Q { get; set; }
        public int R { get; set; }
        /// <summary>What pioneers may build here (yishuv).</summary>
        public List<string> Kinds { get; set; } = new List<string>();
        /// <summary>The historical founding date: the objective card's reference date (PR 6).</summary>
        public DateTime Founded { get; set; }
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "";
        /// <summary>The person who signed this row off: a GitHub handle, written only by that person (owner, 15 October 2026,
        /// #375: "Yes, humans only"; scripts/oss/reviewed_by_guard.py). Empty when no one has; with <see cref="ReviewedOn"/>.</summary>
        public string ReviewedBy { get => reviewedBy ?? ""; set => reviewedBy = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank sign-off is an error.</summary>
        [YamlIgnore] public bool ReviewedByGiven => reviewedBy != null;
        string? reviewedBy;
        /// <summary>The day of the sign-off, YYYY-MM-DD, as the file writes it (read as text, so the loader checks the same
        /// shape as tools/review_record.py and the map's readers; YamlDotNet's own date parsing would take "15 October 2026");
        /// set exactly when <see cref="ReviewedBy"/> is.</summary>
        public string ReviewedOn { get => reviewedOn ?? ""; set { reviewedOn = value ?? ""; ReviewedOnGiven = true; } }
        /// <summary>True when the file has the key at all, even blank.</summary>
        [YamlIgnore] public bool ReviewedOnGiven { get; private set; }
        string? reviewedOn;
    }

    /// <summary>A city's start values (cities.yaml, GDD "Cities and construction"). Counts are population units (D-A1).</summary>
    public sealed class CityStart
    {
        /// <summary>A settlement id of map/settlements_1948.yaml.</summary>
        public string Id { get; set; } = "";
        public int HousingCapacity { get; set; }
        public int JobSlots { get; set; }
        public int Civilians { get; set; }
        /// <summary>The civilians in jobs at the start: part of <see cref="Civilians"/>.</summary>
        public int Employed { get; set; }
        /// <summary>Training facilities in the city (military_camp, hakhshara). PR 4 uses them (D-D5).</summary>
        public List<string> Facilities { get; set; } = new List<string>();
    }

    /// <summary>A population or unit type (unit_types.yaml, GDD "Population and units"). `population`: a count per city;
    /// `unit`: a movable unit (D-A2).</summary>
    public sealed class UnitType
    {
        public string Id { get; set; } = "";
        public string Kind { get; set; } = "";
        /// <summary>Where a civilian trains to become this type (military_camp or hakhshara; GDD "Progression"), or "" for a
        /// type nobody trains into (R-TRAIN-001).</summary>
        public string Facility { get; set; } = "";
        /// <summary>Turns a cohort trains, counting the turn it was ordered in (R-TRAIN-003). Only with a facility.</summary>
        public int TrainingTurns { get; set; }
        /// <summary>IL millions per population unit trained, paid when training starts (R-TRAIN-001). Only with a facility.</summary>
        public double TrainingCost { get; set; }
    }

    /// <summary>One stretch of a budget line's ownership (rules.yaml budget_owners): from this day (inclusive) the office owns the line.</summary>
    public sealed class BudgetOwnerTerm
    {
        public DateTime From { get; set; }
        public string Office { get; set; } = "";
    }

    /// <summary>Price and effect of one map action (rules.yaml map_actions).</summary>
    public sealed class MapActionRule
    {
        /// <summary>Treasury cost, in treasury units.</summary>
        public double Cost { get; set; }
        /// <summary>Change to the housing indicator (absorption).</summary>
        public double Housing { get; set; }
        /// <summary>Change to the morale indicator (public support).</summary>
        public double Morale { get; set; }
        /// <summary>The phrase the player reads in a sentence ("a transit camp"); required.</summary>
        public string Name { get; set; } = "";
        /// <summary>First day the action may be placed (inclusive); null when it is open from the start (chapters spec 3.2).</summary>
        public DateTime? From { get; set; }
        /// <summary>First day it may no longer be placed (exclusive); null while it still may.</summary>
        public DateTime? To { get; set; }
    }

    /// <summary>Money the sim tracks (model.yaml money): treasury, debt and fx_reserves, each with the name and currency
    /// prefix the player sees ("IL " for Israeli pounds, "$" for foreign currency; amounts are in millions).</summary>
    public sealed class MoneyAccount
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Prefix { get; set; } = "";
    }

    /// <summary>An origin group of immigrants (aliyah.yaml regions): the key of AliyahPeriod.Origins.</summary>
    public sealed class Region
    {
        public string Id { get; set; } = "";
        /// <summary>Display name shown to the player (Cause text); required.</summary>
        public string Name { get; set; } = "";
        /// <summary>First day the origin group exists (inclusive); null when it exists from the start (chapters spec 3.2).</summary>
        public DateTime? From { get; set; }
        /// <summary>First day it no longer exists (exclusive, like every window); null while it still does.</summary>
        public DateTime? To { get; set; }
    }

    public sealed class AliyahPeriod
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Dictionary<string, double> Origins { get; set; } = new Dictionary<string, double>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
        /// <summary>The person who signed this row off: a GitHub handle, written only by that person (owner, 15 October 2026,
        /// #375: "Yes, humans only"; scripts/oss/reviewed_by_guard.py). Empty when no one has; with <see cref="ReviewedOn"/>.</summary>
        public string ReviewedBy { get => reviewedBy ?? ""; set => reviewedBy = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank sign-off is an error.</summary>
        [YamlIgnore] public bool ReviewedByGiven => reviewedBy != null;
        string? reviewedBy;
        /// <summary>The day of the sign-off, YYYY-MM-DD, as the file writes it (read as text, so the loader checks the same
        /// shape as tools/review_record.py and the map's readers; YamlDotNet's own date parsing would take "15 October 2026");
        /// set exactly when <see cref="ReviewedBy"/> is.</summary>
        public string ReviewedOn { get => reviewedOn ?? ""; set { reviewedOn = value ?? ""; ReviewedOnGiven = true; } }
        /// <summary>True when the file has the key at all, even blank.</summary>
        [YamlIgnore] public bool ReviewedOnGiven { get; private set; }
        string? reviewedOn;
    }

    public sealed class PopulationAnchor
    {
        public DateTime Date { get; set; }
        public double Jews { get; set; }
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
        /// <summary>The person who signed this row off: a GitHub handle, written only by that person (owner, 15 October 2026,
        /// #375: "Yes, humans only"; scripts/oss/reviewed_by_guard.py). Empty when no one has; with <see cref="ReviewedOn"/>.</summary>
        public string ReviewedBy { get => reviewedBy ?? ""; set => reviewedBy = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank sign-off is an error.</summary>
        [YamlIgnore] public bool ReviewedByGiven => reviewedBy != null;
        string? reviewedBy;
        /// <summary>The day of the sign-off, YYYY-MM-DD, as the file writes it (read as text, so the loader checks the same
        /// shape as tools/review_record.py and the map's readers; YamlDotNet's own date parsing would take "15 October 2026");
        /// set exactly when <see cref="ReviewedBy"/> is.</summary>
        public string ReviewedOn { get => reviewedOn ?? ""; set { reviewedOn = value ?? ""; ReviewedOnGiven = true; } }
        /// <summary>True when the file has the key at all, even blank.</summary>
        [YamlIgnore] public bool ReviewedOnGiven { get; private set; }
        string? reviewedOn;
    }

    public sealed class EventCondition { public string Indicator { get; set; } = ""; public double? Min { get; set; } public double? Max { get; set; } }

    /// <summary>An event's precondition: <see cref="Event"/> must have been resolved with <see cref="Option"/>.</summary>
    public sealed class EventRequirement { public string Event { get; set; } = ""; public string Option { get; set; } = ""; }

    public sealed class OptionEffect { public string Target { get; set; } = ""; public double Delta { get; set; } }

    public sealed class EventOption
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public bool Historical { get; set; }
        public bool Recommended { get; set; }
        public List<OptionEffect> Effects { get; set; } = new List<OptionEffect>();
        public int? SetRung { get; set; }
        /// <summary>Policies this option sets, by id, to a level in [0, 1].</summary>
        public Dictionary<string, double> SetPolicy { get; set; } = new Dictionary<string, double>();
    }

    public sealed class GameEvent
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
        public string Anchor { get; set; } = "soft";
        public string Office { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public List<EventCondition> Conditions { get; set; } = new List<EventCondition>();
        /// <summary>Eligible only once this other event was resolved with this option (null: no requirement).</summary>
        public EventRequirement? Requires { get; set; }
        public List<EventOption> Options { get; set; } = new List<EventOption>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
        /// <summary>The person who signed this row off: a GitHub handle, written only by that person (owner, 15 October 2026,
        /// #375: "Yes, humans only"; scripts/oss/reviewed_by_guard.py). Empty when no one has; with <see cref="ReviewedOn"/>.</summary>
        public string ReviewedBy { get => reviewedBy ?? ""; set => reviewedBy = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank sign-off is an error.</summary>
        [YamlIgnore] public bool ReviewedByGiven => reviewedBy != null;
        string? reviewedBy;
        /// <summary>The day of the sign-off, YYYY-MM-DD, as the file writes it (read as text, so the loader checks the same
        /// shape as tools/review_record.py and the map's readers; YamlDotNet's own date parsing would take "15 October 2026");
        /// set exactly when <see cref="ReviewedBy"/> is.</summary>
        public string ReviewedOn { get => reviewedOn ?? ""; set { reviewedOn = value ?? ""; ReviewedOnGiven = true; } }
        /// <summary>True when the file has the key at all, even blank.</summary>
        [YamlIgnore] public bool ReviewedOnGiven { get; private set; }
        string? reviewedOn;
        /// <summary>Path relative to the content root, set by the loader (not read from YAML).</summary>
        [YamlIgnore] public string File { get; set; } = "";
    }

    /// <summary>One hex of content/map/hexes.json (written by tools/hexgrid.py; axial q, r, pointy-top).</summary>
    public sealed class MapHex
    {
        public int Q { get; set; }
        public int R { get; set; }
        public double Lon { get; set; }
        public double Lat { get; set; }
        public double ElevM { get; set; }
        public bool Land { get; set; }
        public string Biome { get; set; } = "";
    }

    /// <summary>The hex that owns a settlement (content/map/settlement_hexes.yaml, written by tools/settlement_hexes.py
    /// with the hexgrid.settlement_hex rule).</summary>
    public sealed class SettlementHex
    {
        public string Id { get; set; } = "";
        public int Q { get; set; }
        public int R { get; set; }
    }

    public sealed class Content
    {
        public List<Office> Offices = new List<Office>();
        public List<Party> Parties = new List<Party>();
        public List<Person> People = new List<Person>();
        public List<Legislature> Legislatures = new List<Legislature>();
        public List<Government> Governments = new List<Government>();
        /// <summary>The eras in order (eras.yaml).</summary>
        public List<Era> Eras = new List<Era>();
        /// <summary>The era that contains <paramref name="date"/> (From inclusive, To exclusive), or null outside every era.</summary>
        public Era? EraAt(DateTime date) => Eras.FirstOrDefault(e => date >= e.From && date < e.To);
        /// <summary>The system switches in force on <paramref name="date"/>; the defaults outside every era.</summary>
        public EraSystems SystemsAt(DateTime date) => EraAt(date)?.Systems ?? new EraSystems();
        /// <summary>The game's loop (eras.yaml systems.loop, the same in every era): legacy or gdd. R-LOOP-001.</summary>
        public string Loop => Eras.Count == 0 ? EraSystems.Legacy : Eras[0].Systems.Loop;
        /// <summary>True when the GDD v0.1 loop runs.</summary>
        public bool IsGdd => Loop == EraSystems.Gdd;
        /// <summary>The ruleset a save names (SaveGame.Ruleset): "legacy" or "gdd-0.1".</summary>
        public string Ruleset => IsGdd ? SaveGame.GddRuleset : SaveGame.LegacyRuleset;
        public List<Indicator> Indicators = new List<Indicator>();
        public List<Policy> Policies = new List<Policy>();
        public List<Effect> Effects = new List<Effect>();
        public List<Situation> Situations = new List<Situation>();
        public List<VoterGroup> VoterGroups = new List<VoterGroup>();
        public List<Actor> Actors = new List<Actor>();
        ModelView[] _views = new ModelView[0];
        DateTime[] _cuts = new DateTime[0];
        /// <summary>Computes the windows' segments (chapters design 3.2): a sorted array of the days a model record begins or ends, and
        /// the records active in each segment. Called once by the loader; <see cref="At"/> then only does a binary search, so tests
        /// that run in parallel share nothing mutable. With no window anywhere there is one segment, the full lists.</summary>
        internal void BuildWindows()
        {
            var cuts = new SortedSet<DateTime>();
            void Cut(DateTime? a, DateTime? b) { if (a.HasValue) cuts.Add(a.Value); if (b.HasValue) cuts.Add(b.Value); }
            foreach (var x in Indicators) Cut(x.From, x.To);
            foreach (var x in Policies) Cut(x.From, x.To);
            foreach (var x in Effects) Cut(x.From, x.To);
            foreach (var x in Situations) Cut(x.From, x.To);
            foreach (var x in VoterGroups) Cut(x.From, x.To);
            foreach (var x in Actors) Cut(x.From, x.To);
            _cuts = cuts.ToArray();
            if (_cuts.Length == 0) { _views = new[] { new ModelView(Indicators, Policies, Effects, Situations, VoterGroups, Actors) }; return; }
            _views = new ModelView[_cuts.Length + 1];
            for (int k = 0; k < _views.Length; k++)
            {
                var d = k == 0 ? DateTime.MinValue : _cuts[k - 1];
                _views[k] = new ModelView(Indicators.Where(x => x.ActiveOn(d)).ToArray(), Policies.Where(x => x.ActiveOn(d)).ToArray(),
                    Effects.Where(x => x.ActiveOn(d)).ToArray(), Situations.Where(x => x.ActiveOn(d)).ToArray(),
                    VoterGroups.Where(x => x.ActiveOn(d)).ToArray(), Actors.Where(x => x.ActiveOn(d)).ToArray());
            }
        }

        /// <summary>The model records that exist on <paramref name="date"/>. A record with no window is in every view.</summary>
        public ModelView At(DateTime date)
        {
            int lo = 0, hi = _cuts.Length;   // the number of cuts on or before the date
            while (lo < hi) { int mid = (lo + hi) / 2; if (_cuts[mid] <= date) lo = mid + 1; else hi = mid; }
            return _views[lo];
        }

        public Rules Rules = new Rules();
        /// <summary>The complete rules of each era that has an override file (content/rules/&lt;era&gt;.yaml): rules.yaml with that
        /// file's keys merged in. Built once at load and never changed. An era without a file is not in this map.</summary>
        public Dictionary<string, Rules> EraRules = new Dictionary<string, Rules>();
        /// <summary>The rules in force on <paramref name="date"/>: the era's own rules when its override file exists, else
        /// <see cref="Rules"/> (the founding era has no override, so its rules are rules.yaml itself).</summary>
        public Rules RulesAt(DateTime date)
        {
            var era = EraAt(date);
            return era != null && EraRules.TryGetValue(era.Id, out var r) ? r : Rules;
        }
        public List<AliyahPeriod> Aliyah = new List<AliyahPeriod>();
        /// <summary>Origin groups of the aliyah series, with display names (aliyah.yaml regions).</summary>
        public List<Region> Regions = new List<Region>();
        /// <summary>Display names and currency prefixes of the treasury, debt and foreign currency reserves (model.yaml money).</summary>
        public List<MoneyAccount> Money = new List<MoneyAccount>();
        public List<PopulationAnchor> PopulationAnchors = new List<PopulationAnchor>();
        public List<GameEvent> Events = new List<GameEvent>();
        List<MapHex> _hexes = new List<MapHex>();
        Dictionary<(int, int), MapHex> _hexIndex = new Dictionary<(int, int), MapHex>();

        /// <summary>The map. Assign it (the index behind <see cref="HexAt"/> is rebuilt on assignment, so it cannot go stale).</summary>
        public List<MapHex> Hexes
        {
            get => _hexes;
            set
            {
                _hexes = value;
                var index = new Dictionary<(int, int), MapHex>();
                foreach (var h in value) index[(h.Q, h.R)] = h;
                _hexIndex = index;
            }
        }
        public List<SettlementHex> SettlementHexes = new List<SettlementHex>();
        /// <summary>The display name of each settlement id (map/settlements_1948.yaml name), for text the player reads.</summary>
        public Dictionary<string, string> SettlementNames = new Dictionary<string, string>();
        /// <summary>The GDD cities' start values (cities.yaml), in file order. Empty in the legacy loop.</summary>
        public List<CityStart> Cities = new List<CityStart>();
        /// <summary>The GDD population and unit types (unit_types.yaml). Empty in the legacy loop.</summary>
        public List<UnitType> UnitTypes = new List<UnitType>();
        /// <summary>The GDD designated sites (sites.yaml), in file order. Empty in the legacy loop.</summary>
        public List<Site> Sites = new List<Site>();
        /// <summary>SHA-256 (lowercase hex) over every file the loader read, in read order, with CRLF read as LF. A save
        /// records it; loading a save made with other content shows a warning (spec section 7).</summary>
        public string Hash = "";

        /// <summary>The hex at axial (q, r), or null when the map has none there.</summary>
        public MapHex? HexAt(int q, int r)
        {
            return _hexIndex.TryGetValue((q, r), out var hex) ? hex : null;
        }
    }

    public sealed class ContentException : Exception
    {
        public string File { get; }
        public string Field { get; }
        public ContentException(string file, string field, string message) : base($"{file}: {field}: {message}")
        { File = file; Field = field; }
    }
}
