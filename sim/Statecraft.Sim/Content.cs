using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Statecraft.Sim
{
    // Plain data classes filled by YamlDotNet (UnderscoredNamingConvention: SeatsByParty <- seats_by_party).

    public sealed class Source { public string Url { get; set; } = ""; public DateTime Checked { get; set; } }

    public sealed class Office
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        /// <summary>Historical record only: no Cabinet seat, player appointment or gameplay responsibility. Defaults false for existing offices.</summary>
        public bool HistoricalOnly { get; set; }
        /// <summary>Day the office was abolished (exclusive end, like a term's `to`); null while it still exists.</summary>
        public DateTime? Until { get; set; }
    }

    public sealed class Party { public string Id { get; set; } = ""; public string Name { get; set; } = ""; }

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
    }

    public sealed class Legislature
    {
        public string Id { get; set; } = "";
        public string Body { get; set; } = "";
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
    }

    public sealed class Government
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime From { get; set; }
        public DateTime? To { get; set; }
        public List<string> Coalition { get; set; } = new List<string>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class Indicator
    {
        public string Id { get; set; } = "";
        public double Base { get; set; }
        /// <summary>Display name shown to the player (Cause text); required.</summary>
        public string Name { get; set; } = "";
    }

    public sealed class Policy
    {
        public string Id { get; set; } = "";
        public double Default { get; set; }
        public string Office { get; set; } = "";
        /// <summary>Display name shown to the player (Cause text); required.</summary>
        public string Name { get; set; } = "";
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
    }

    public sealed class VoterGroup { public string Id { get; set; } = ""; public Dictionary<string, double> Weights { get; set; } = new Dictionary<string, double>(); }

    public sealed class Actor { public string Id { get; set; } = ""; public string Name { get; set; } = ""; public double Standing { get; set; } }

    public sealed class Rules
    {
        public DateTime Start { get; set; }
        public int StartRung { get; set; } = 1;
        public double AttentionPerTurn { get; set; }
        public Dictionary<string, double> AttentionCost { get; set; } = new Dictionary<string, double>();
        public double RungAttentionStep { get; set; }
        public int CeasefireWeeks { get; set; }
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
    }

    public sealed class AliyahPeriod
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Dictionary<string, double> Origins { get; set; } = new Dictionary<string, double>();
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
    }

    public sealed class PopulationAnchor
    {
        public DateTime Date { get; set; }
        public double Jews { get; set; }
        public List<Source> Sources { get; set; } = new List<Source>();
        public string Status { get; set; } = "draft";
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
        public List<Indicator> Indicators = new List<Indicator>();
        public List<Policy> Policies = new List<Policy>();
        public List<Effect> Effects = new List<Effect>();
        public List<Situation> Situations = new List<Situation>();
        public List<VoterGroup> VoterGroups = new List<VoterGroup>();
        public List<Actor> Actors = new List<Actor>();
        public Rules Rules = new Rules();
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
