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
        /// <summary>The feminine Hebrew title, shown for a woman in the office (the Knesset records write שרת העבודה); every
        /// office with a Hebrew title has one, since the player can order a woman into any office (owner on #861, 17 October
        /// 2026). Sourced in <see cref="TitleHeFSources"/>. Needs <see cref="TitleHe"/>.</summary>
        public string TitleHeF { get; set; } = "";
        public List<Source> TitleHeFSources { get; set; } = new List<Source>();
        /// <summary>As <see cref="TitleHeReview"/>, for <see cref="TitleHeF"/>.</summary>
        public string TitleHeFReview { get => titleHeFReview ?? ""; set => titleHeFReview = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank flag is an error, not a review.</summary>
        [YamlIgnore] public bool TitleHeFReviewGiven => titleHeFReview != null;
        string? titleHeFReview;
        /// <summary>The Hebrew title to show for <paramref name="holder"/>: <see cref="TitleHeF"/> when the person's
        /// <see cref="Person.TitleHeForm"/> is feminine (the loader makes sure every office such a person holds has one),
        /// else <see cref="TitleHe"/>. Empty when the office has no Hebrew title (the UI then keeps the English).</summary>
        public string TitleHeFor(Person? holder) => holder?.TitleHeForm == TitleHeForms.Feminine ? TitleHeF : TitleHe;
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

    /// <summary>The values of people.yaml's `title_he_form`.</summary>
    public static class TitleHeForms
    {
        /// <summary>The person's Hebrew office titles take the feminine form, <see cref="Office.TitleHeF"/> (שרת העבודה).</summary>
        public const string Feminine = "feminine";
    }

    public sealed class Person
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NameHe { get; set; } = "";
        /// <summary>"agent-checked" while <see cref="NameHe"/> waits for a native speaker's review (#564, #862); empty once
        /// reviewed. Its sources are biography_sources.name_he.</summary>
        public string NameHeReview { get => nameHeReview ?? ""; set => nameHeReview = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank flag is an error, not a review.</summary>
        [YamlIgnore] public bool NameHeReviewGiven => nameHeReview != null;
        string? nameHeReview;
        /// <summary>"feminine" when the person's Hebrew office titles take the feminine form (<see cref="Office.TitleHeF"/>),
        /// sourced in biography_sources.title_he_form; empty (the key left out) for the masculine <see cref="Office.TitleHe"/>.
        /// Display only: no rule reads it.</summary>
        public string TitleHeForm { get => titleHeForm ?? ""; set => titleHeForm = value ?? ""; }
        /// <summary>True when the file has the key at all, even blank: a blank form is an error, not the masculine.</summary>
        [YamlIgnore] public bool TitleHeFormGiven => titleHeForm != null;
        string? titleHeForm;
        /// <summary>Biographical birth date; optional until the sourced person data is complete.</summary>
        public DateTime? Born { get; set; }
        /// <summary>Sources for each biography value (name_he, title_he_form, born, died), separate from dated office-term sources.</summary>
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
        /// otherwise. Its numbers are design values the owner sets (R-LOOP-005).</summary>
        public MvpRules? Mvp { get; set; }
    }

    /// <summary>rules.yaml mvp: the GDD v0.1 slice's city rules (Phase 1 PR 3).</summary>
    public sealed class MvpRules
    {
        /// <summary>People in one population unit (D-A1: a unit is a group).</summary>
        public int PeoplePerUnit { get; set; }
        /// <summary>Money each employed unit earns a year, in IL millions (R-CITY-002).</summary>
        public double IncomePerJobPerYear { get; set; }
        /// <summary>What an employed craftsman earns, as a multiple of <see cref="IncomePerJobPerYear"/> (R-CRAFT-002, Q-CRAFT-2).</summary>
        public double CraftsmanIncomeMultiple { get; set; }
        /// <summary>Annual civilian-place upkeep in IL millions (R-MONEY-002). Required in the GDD loop; the MVP values are 0.</summary>
        public double? CityUpkeepPerYear { get; set; }
        public double? HousingUpkeepPerYear { get; set; }
        public double? IndustryUpkeepPerYear { get; set; }
        public double? TransitCampUpkeepPerYear { get; set; }
        public double? MilitaryCampUpkeepPerYear { get; set; }
        public double? HakhsharaUpkeepPerYear { get; set; }
        /// <summary>The day the cities start taking arrivals: the census day their start values count (R-CITY-003, Q-ARRV-3).</summary>
        public DateTime CityArrivalsFrom { get; set; }
        /// <summary>The day the GDD game ends (R-LOOP-004; D-E5, 15 January 1952 in the real content), or null for the last
        /// era's end. Read from rules.yaml through <see cref="Content.End"/>; the same in every era.</summary>
        public DateTime? End { get; set; }
        /// <summary>Turns a "build yishuv" takes, counting the turn it was ordered in (R-SITE-003).</summary>
        public int BuildYishuvTurns { get; set; }
        /// <summary>IL millions a "build yishuv" costs, paid in full when it starts (R-SITE-001).</summary>
        public double BuildYishuvCost { get; set; }
        /// <summary>Turns a "build camp" takes, counting the turn it was ordered in (R-CAMP-001; Q-CAMP-3). Required in the GDD
        /// loop, at least 1.</summary>
        public int? BuildCampTurns { get; set; }
        /// <summary>IL millions a "build camp" costs, paid in full when it starts (R-CAMP-001). Required in the GDD loop.</summary>
        public double? BuildCampCost { get; set; }
        /// <summary>Turns plowing a field takes, counting the turn it was ordered in (R-FIELD-001, as R-SITE-003).</summary>
        public int PlowFieldsTurns { get; set; }
        /// <summary>IL millions plowing a field costs, paid in full when it starts (R-FIELD-001, as R-SITE-001). Required in the
        /// GDD loop, so a missing key never makes plowing free.</summary>
        public double? PlowFieldsCost { get; set; }
        /// <summary>Turns draining a swamp takes, counting the turn it was ordered in (R-SWAMP-001, as R-SITE-003; Q-SWAMP-1).
        /// Required in the GDD loop, at least 1.</summary>
        public int? DrySwampTurns { get; set; }
        /// <summary>IL millions draining a swamp costs, paid in full when it starts (R-SWAMP-001, as R-SITE-001; Q-SWAMP-1).
        /// Required in the GDD loop, so a missing key never makes draining free.</summary>
        public double? DrySwampCost { get; set; }
        /// <summary>Food units a plowed field adds to the national stock each turn, in step 6 (R-FIELD-002; Q-FIELD-3). Required
        /// in the GDD loop, so a missing key is never read as 0.</summary>
        public int? FieldFoodPerTurn { get; set; }
        /// <summary>Points for each completed objective (R-OBJ-002; D-B6, #456).</summary>
        public int? ObjectivePoints { get; set; }
        /// <summary>Bonus points for each calendar month an objective is completed before its reference date's month (R-OBJ-002; D-B2, #456).</summary>
        public int? ObjectiveBonusPerMonth { get; set; }
        /// <summary>The most bonus points one objective can earn (R-OBJ-002; D-B2, #456).</summary>
        public int? ObjectiveBonusCap { get; set; }
        /// <summary>How far food_supply falls when the Food stock is empty, against a full cover (R-FOOD-003; maintainer
        /// decision of 16 October 2026, #382, option (b)). Required in the GDD loop, 0 to 1.</summary>
        public double? KFood { get; set; }
        /// <summary>The months of the nation's need the Food stock holds when full: the start stock and the level the monthly
        /// imports keep (R-FOOD-001, R-FOOD-002). Required in the GDD loop, at least 1.</summary>
        public int? CoverMonths { get; set; }
        /// <summary>Turns of work after which a halutz unit becomes a veteran_halutz (R-VET-001; D-D9, Q-VET-1). Required in
        /// the GDD loop, at least 1.</summary>
        public int? VeteranHalutzService { get; set; }
        /// <summary>Turns of progress a veteran's project gains each turn, against a halutz's 1 (R-VET-002; D-D9, Q-VET-2).
        /// Required in the GDD loop, at least 1.</summary>
        public int? VeteranProgress { get; set; }
        /// <summary>Every city's housing condition on the first day (R-HOUSE-001; D-D2, Q-START-3: one value for all the
        /// cities). Required in the GDD loop, 0 to 1.</summary>
        public double? HousingStartCondition { get; set; }
        /// <summary>How far a city's housing condition falls in a turn when all its people are beyond its housing; a turn takes
        /// this times the city's overflow share (R-HOUSE-002; D-D1, Q-HOUSE-1). Required in the GDD loop, 0 to 1.</summary>
        public double? OverflowConditionLoss { get; set; }
        /// <summary>Consecutive turn ends with Money and Food both exhausted that end the game (R-END-002; D-B5, Q-DEF-2).
        /// Required in the GDD loop, at least 1.</summary>
        public int? DefeatTurns { get; set; }
        /// <summary>The debt, in IL millions, at or above which Money is exhausted (R-END-001; D-B5, Q-DEF-1). Required in the
        /// GDD loop, a finite number >= 0.</summary>
        public double? DebtCeiling { get; set; }
        /// <summary>A minister whose loyalty is below this carries a delegated decision out half-heartedly (R-EVT-003; #107,
        /// owner, 18 October 2026). Required in the GDD loop, 0 to 10 (0: nobody is below it).</summary>
        public int? DelegationLoyaltyFloor { get; set; }
        /// <summary>The share of each numeric effect a delegated option applies when its minister is below the loyalty floor
        /// (R-EVT-003; #107). Required in the GDD loop, 0 to 1.</summary>
        public double? DisloyalEffectShare { get; set; }
    }

    /// <summary>A kind of city project (projects.yaml; GDD "Cities and construction"; R-PROJ-001 to R-PROJ-003): permanent
    /// housing, the transit camp (D-D3, Q-HOUSE-3) or industry. Every number is a design value (Q-PROJ-1).</summary>
    public sealed class ProjectKind
    {
        public string Id { get; set; } = "";
        /// <summary>The phrase the player reads in a sentence ("a transit camp").</summary>
        public string Name { get; set; } = "";
        /// <summary>IL millions, paid in full at the start (Q-PROJ-1).</summary>
        public double Cost { get; set; }
        /// <summary>Turns to build, the turn of the order the first (D-A3).</summary>
        public int Turns { get; set; }
        /// <summary>Housing capacity added to the city when done, in population units.</summary>
        public int Capacity { get; set; }
        /// <summary>The change to the city's housing condition when done (R-HOUSE-001; kept from 0 to 1).</summary>
        public double Condition { get; set; }
        /// <summary>Job slots added to the city when done, in population units.</summary>
        public int JobSlots { get; set; }
        /// <summary>A city resource the city must have (cities.yaml `resources:`), or "" for none: a specialised industry such as
        /// the winery, which needs vineyards (R-PROJ-005, D-D8).</summary>
        public string Requires { get; set; } = "";
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
        /// <summary>What pioneers may build here: one or more of yishuv, field, swamp, military_camp, hakhshara (R-SITE-004).</summary>
        public List<string> Kinds { get; set; } = new List<string>();
        /// <summary>A yishuv site's historical founding date: the objective card's reference date (PR 6). Unset for other kinds.</summary>
        public DateTime Founded { get; set; }
        /// <summary>A camp site's city (cities.yaml id), the city a built camp gives the facility to (Q-CAMP-1, Phase 4 PR 3).
        /// Empty for other kinds.</summary>
        public string City { get; set; } = "";
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

    /// <summary>One start value of a city (cities.yaml), with what it is (R-CITY-006): `sourced` (read from the source named),
    /// `derived` (computed from sourced values), `wikipedia-only` (an owner-accepted Wikipedia figure) or `design` (a
    /// labelled design value, not a historical figure; R-LOOP-005). The first three name their sources, with checked dates,
    /// and the day (YYYY-MM-DD) or month (YYYY-MM) the value describes.</summary>
    public sealed class CityValue
    {
        /// <summary>Read as a decimal, so a share converts to units exactly on every runtime (R-CITY-007).</summary>
        public decimal Value { get; set; }
        public string Status { get; set; } = "";
        /// <summary>The day or month the value describes, as the file writes it ("" for a design value without one).</summary>
        public string Date { get; set; } = "";
        public List<Source> Sources { get; set; } = new List<Source>();
    }

    /// <summary>A city's local resource (cities.yaml `resources:`; R-PROJ-005, D-D8: "A winery requires areas for grapes"): an
    /// id a project kind's `requires` names, its sources (each with a url and a checked date), and what the sources leave
    /// open (`to_verify`, as in map/routes_1948.yaml).</summary>
    public sealed class CityResource
    {
        public string Id { get; set; } = "";
        public List<Source> Sources { get; set; } = new List<Source>();
        [YamlMember(Alias = "to_verify")] public string ToVerify { get; set; } = "";
    }

    /// <summary>A city's start values (cities.yaml, GDD "Cities and construction"). The file gives people and shares, each
    /// with its status (R-CITY-006); the loader turns them into the population units the sim counts (D-A1, R-CITY-007).</summary>
    public sealed class CityStart
    {
        /// <summary>A settlement id of map/settlements_1948.yaml.</summary>
        public string Id { get; set; } = "";
        /// <summary>Residents, all groups (the census basis; the owner, 15 October 2026, on #569).</summary>
        public CityValue? People { get; set; }
        /// <summary>The share of <see cref="People"/> in work at the start, from 0 to 1.</summary>
        public CityValue? EmployedShare { get; set; }
        /// <summary>Housing, in population units.</summary>
        [YamlMember(Alias = "housing_capacity")] public CityValue? Housing { get; set; }
        /// <summary>Job slots, in population units.</summary>
        [YamlMember(Alias = "job_slots")] public CityValue? Jobs { get; set; }
        /// <summary>Training facilities in the city (military_camp, hakhshara). PR 4 uses them (D-D5).</summary>
        public List<string> Facilities { get; set; } = new List<string>();
        /// <summary>The city's local resources (R-PROJ-005, D-D8), each sourced, which a specialised project may require.</summary>
        public List<CityResource> Resources { get; set; } = new List<CityResource>();

        // The units the sim starts from, set by the loader (R-CITY-007).
        [YamlIgnore] public int HousingCapacity { get; internal set; }
        [YamlIgnore] public int JobSlots { get; internal set; }
        [YamlIgnore] public int Civilians { get; internal set; }
        /// <summary>The civilians in jobs at the start: part of <see cref="Civilians"/>.</summary>
        [YamlIgnore] public int Employed { get; internal set; }
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
        /// <summary>A military type's side (units_1948.yaml's side ids), or "" for a type that is not military (R-UNIT-001).</summary>
        public string Side { get; set; } = "";
        /// <summary>A military type's class: infantry, artillery or armour, or "" for a type that is not military (R-UNIT-001).</summary>
        public string Class { get; set; } = "";
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

    /// <summary>One precondition of an event: <see cref="Event"/> was resolved with <see cref="Option"/>, or, with
    /// <see cref="Diverged"/>, with any option but its historical one (R-ALT-001, R-ALT-002).</summary>
    public sealed class EventRequirement
    {
        public string Event { get; set; } = "";
        public string Option { get; set; } = "";
        public bool Diverged { get; set; }
    }

    /// <summary>R-ALT-010: alternative wording of a reconvergence event for a course that left the record. Options and
    /// effects never vary.</summary>
    public sealed class EventVariant
    {
        public List<EventRequirement> When { get; set; } = new List<EventRequirement>();
        /// <summary>Empty: the event's own title.</summary>
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public List<Source> Sources { get; set; } = new List<Source>();
    }

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
        /// <summary>R-EFX-003: units this option grants, free, in list order (D-H2, Q-HFX-2).</summary>
        public List<UnitGrant> GrantUnits { get; set; } = new List<UnitGrant>();
        /// <summary>R-EFX-004: changes to a city's housing capacity, job slots or housing condition, in list order (D-H2, Q-HFX-1).</summary>
        public List<CityEffect> CityEffects { get; set; } = new List<CityEffect>();
        /// <summary>Who put this non-historical option forward, and on what record (R-ALT-003). Null when absent.</summary>
        public OptionAlternative? Alternative { get; set; }
        /// <summary>The event whose historical course this consequence option rejoins (R-ALT-009), or empty.</summary>
        public string Rejoin { get; set; } = "";
    }

    /// <summary>An option's grant of <see cref="N"/> units of a unit_types.yaml type of kind unit (R-EFX-003). They stand at
    /// <see cref="At"/>, a cities.yaml id, or, when it is empty, at the city that has the military camp. A site is refused
    /// until sites carry a date (R-EFX-006).</summary>
    public sealed class UnitGrant
    {
        public string Type { get; set; } = "";
        public int N { get; set; }
        public string At { get; set; } = "";
    }

    /// <summary>An option's change to one field of a city (R-EFX-004): housing_capacity or job_slots by whole units, or the
    /// housing condition by a number from -1 to 1. A double, so one key carries both; the loader checks each field's kind.</summary>
    public sealed class CityEffect
    {
        public string City { get; set; } = "";
        public string Field { get; set; } = "";
        public double Delta { get; set; }
    }

    /// <summary>Who put forward a non-historical option, and on what record (R-ALT-003). `proposed` and `debated` name a
    /// body or office in words (never a person), the date and sources; `design` is the GDD's "design assumption": nobody is
    /// recorded proposing it, so it names nobody and no date.</summary>
    public sealed class OptionAlternative
    {
        public const string Proposed = "proposed", Debated = "debated", Design = "design";
        public string Basis { get; set; } = "";
        public string By { get; set; } = "";
        public DateTime? Date { get; set; }
        public List<Source> Sources { get; set; } = new List<Source>();
        /// <summary>True for an alternative contemporaries are recorded proposing or debating (the Ferguson test).</summary>
        [YamlIgnore] public bool IsConsidered => Basis == Proposed || Basis == Debated;
    }

    /// <summary>The kinds of event (R-ALT-005): the record, a card only on a road not taken, and a real outside
    /// event that happened regardless.</summary>
    public static class EventKinds
    {
        public const string Historical = "historical", Consequence = "consequence", Reconvergence = "reconvergence";
    }

    public sealed class GameEvent
    {
        public string Id { get; set; } = "";
        /// <summary>historical (the default), consequence (R-ALT-005) or reconvergence (R-ALT-010).</summary>
        public string Kind { get; set; } = EventKinds.Historical;
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public List<string> Tags { get; set; } = new List<string>();
        public string Anchor { get; set; } = "soft";
        public string Office { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public List<EventCondition> Conditions { get; set; } = new List<EventCondition>();
        /// <summary>Eligible only once every one of these holds (R-ALT-001). Empty: no requirement.</summary>
        public List<EventRequirement> Requires { get; set; } = new List<EventRequirement>();
        public List<EventVariant> Variants { get; set; } = new List<EventVariant>();
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
        /// <summary>The objective (objectives.yaml) whose completion card this event is: it fires only once that objective is
        /// recorded (R-OBJ-003). Empty for every other event.</summary>
        public string Objective { get; set; } = "";
        /// <summary>The designated site (sites.yaml, a swamp site) whose works this event is about: in the GDD loop it fires
        /// only while that site is being worked (R-SWAMP-002; Q-SWAMP-2). The legacy loop reads no sites and ignores it.
        /// Empty for every other event.</summary>
        public string Site { get; set; } = "";
        /// <summary>Path relative to the content root, set by the loader (not read from YAML).</summary>
        [YamlIgnore] public string File { get; set; } = "";
    }

    /// <summary>An objective (objectives.yaml; docs/GDD.md "Historical events on objective completion"). Completing it records
    /// it, adds its points to the score and fires its card (R-OBJ-001 to R-OBJ-003). Its reference date is the sourced date of
    /// the real event the card shows.</summary>
    public sealed class Objective
    {
        public string Id { get; set; } = "";
        /// <summary>The name the player reads (in the Why list).</summary>
        public string Name { get; set; } = "";
        /// <summary>What completes it, checked at the end of every GDD turn (R-OBJ-001).</summary>
        public ObjectiveCondition? CompletesWhen { get; set; }
        /// <summary>The date the real event happened: completing earlier earns the timing bonus (R-OBJ-002).</summary>
        public DateTime ReferenceDate { get; set; }
        /// <summary>The id of the card event (content/events/), which names this objective back in its `objective` key.</summary>
        public string Card { get; set; } = "";
    }

    /// <summary>What completes an objective (objectives.yaml completes_when): exactly one kind.</summary>
    public sealed class ObjectiveCondition
    {
        /// <summary>A designated site (sites.yaml) of kind yishuv: complete once it is built (R-SITE-003).</summary>
        public string SiteBuilt { get; set; } = "";
        /// <summary>The cities have absorbed the immigrants (R-OBJ-004, D-B4).</summary>
        public AbsorbCondition? Absorbed { get; set; }
    }

    /// <summary>R-OBJ-004 (D-B4; owner, 18 October 2026, #738, option (a)): once the Jewish population has reached
    /// <see cref="PopulationAtLeast"/>, the cities' overflow is at most <see cref="OverflowAtMost"/> of the people they house
    /// and their employed civilians at least <see cref="EmployedAtLeast"/> of their civilians. The population gate keeps the
    /// start values (already 41% employed, no overflow) from completing it on the first turn.</summary>
    public sealed class AbsorbCondition
    {
        /// <summary>GameState.TotalPopulation, the national Jewish count, at or above this.</summary>
        public long PopulationAtLeast { get; set; }
        /// <summary>Overflow (R-HOUSE-002) over the housed people (R-HOUSE-003), at or below this share. Required: 0 is a
        /// real value here, so a missing key must not read as 0.</summary>
        public decimal? OverflowAtMost { get; set; }
        /// <summary>Employed over civilians (as R-CITY-005 counts them), at or above this share.</summary>
        public decimal EmployedAtLeast { get; set; }
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
        /// <summary>The day play ends, a boundary the last turn reaches (Simulation.Step): rules.yaml mvp.end in the GDD loop
        /// (R-LOOP-004), otherwise the last era's end.</summary>
        public DateTime End => IsGdd && Rules.Mvp?.End is DateTime e ? e : Eras[Eras.Count - 1].To;
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
        /// <summary>The GDD city project kinds (projects.yaml), in file order. Empty in the legacy loop.</summary>
        public List<ProjectKind> ProjectKinds = new List<ProjectKind>();
        /// <summary>The objectives (objectives.yaml), ordered by id. Read in both loops, since their cards are events; only the
        /// GDD loop completes them. Empty when the content has no objectives.yaml.</summary>
        public List<Objective> Objectives = new List<Objective>();
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
