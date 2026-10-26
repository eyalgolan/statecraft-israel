using System;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    // R-POP-002: enum ordinals are never serialized. Tokens below are an explicit wire contract.
    public enum PopulationStatus { Refugee, FreeCivilian, CivilianInTransit, Trainee, ActiveSoldier, Captured }
    public enum PopulationAssignmentKind { Unassigned, Work, Travel, Reception, Course, Teaching, ActiveService }
    public enum CivilianProfession { None, Farmer, Pioneer, Craftsman }
    public enum CivilianTier { None, Novice, Trained, Veteran, Apprentice, Expert }

    /// <summary>An exact authored binding, not a region inferred from a hex or a captive's invented prison.</summary>
    public sealed class PopulationLocation
    {
        public string Region { get; }
        public int? Q { get; }
        public int? R { get; }
        public string? FacilityOrPort { get; }
        public PopulationLocation(string region, int? q = null, int? r = null, string? facilityOrPort = null)
        {
            if (string.IsNullOrWhiteSpace(region) || q.HasValue != r.HasValue || (facilityOrPort != null && string.IsNullOrWhiteSpace(facilityOrPort)))
                throw new ArgumentException("A region, a complete optional hex and a nonempty optional facility/port are required.");
            PopulationText.ValidateText(region); PopulationText.ValidateText(facilityOrPort);
            Region = region; Q = q; R = r; FacilityOrPort = facilityOrPort;
        }
        internal string Canonical() => PopulationText.Fields(Region, Q, R, FacilityOrPort);
    }

    public sealed class PopulationAssignment
    {
        public static PopulationAssignment Unassigned { get; } = new PopulationAssignment(PopulationAssignmentKind.Unassigned);
        public PopulationAssignmentKind Kind { get; }
        public string? Target { get; }
        public int ProgressTicks { get; }
        public int? TeacherId { get; }
        public PopulationAssignment(PopulationAssignmentKind kind, string? target = null, int progressTicks = 0, int? teacherId = null)
        {
            PopulationText.Assignment(kind); // Reject unknown enum values even before ledger validation.
            if ((kind == PopulationAssignmentKind.Unassigned) != (target == null) || target == ""
                || progressTicks < 0 || teacherId < 0
                || (kind != PopulationAssignmentKind.Course && (progressTicks != 0 || teacherId != null)))
                throw new ArgumentException("Only a course carries progress/teacher; assigned work needs one target.");
            PopulationText.ValidateText(target);
            Kind = kind; Target = target; ProgressTicks = progressTicks; TeacherId = teacherId;
        }
        internal string Canonical() => PopulationText.Fields(PopulationText.Assignment(Kind), Target, ProgressTicks, TeacherId);
    }

    /// <summary>Issued items are distinct from spare Arms. Payment facts are retained independently of current ownership.
    /// This contract makes no refund, replacement or captive-equipment policy.</summary>
    public sealed class PopulationEquipment
    {
        public static PopulationEquipment None { get; } = new PopulationEquipment();
        public string? ToolsId { get; }
        public bool ToolsPaid { get; }
        public string? MilitaryId { get; }
        public bool MilitaryPaid { get; }
        public PopulationEquipment(string? toolsId = null, bool toolsPaid = false, string? militaryId = null, bool militaryPaid = false)
        {
            if ((toolsId != null && string.IsNullOrWhiteSpace(toolsId)) || (militaryId != null && string.IsNullOrWhiteSpace(militaryId))
                || (toolsId != null && toolsId == militaryId))
                throw new ArgumentException("Issued items need distinct nonempty ids.");
            PopulationText.ValidateText(toolsId); PopulationText.ValidateText(militaryId);
            ToolsId = toolsId; ToolsPaid = toolsPaid; MilitaryId = militaryId; MilitaryPaid = militaryPaid;
        }
        internal string Canonical() => PopulationText.Fields(ToolsId, ToolsPaid, MilitaryId, MilitaryPaid);
    }

    /// <summary>Immutable own-population record. Each record represents one population unit, not a named person.
    /// Cross-record/binding validation occurs atomically at the ledger boundary.</summary>
    public sealed class PopulationUnit
    {
        public int Id { get; }
        public PopulationStatus Status { get; }
        public int ReceptionTicks { get; }
        public PopulationLocation? Location { get; }
        public PopulationAssignment Assignment { get; }
        public CivilianProfession Profession { get; }
        public CivilianTier Tier { get; }
        public int WorkXp { get; }
        public string Qualification { get; }
        public int CombatXp { get; }
        public PopulationEquipment Equipment { get; }
        public string? Captor { get; }
        public PopulationLocation? Home { get; }
        public PopulationUnit(int id, PopulationStatus status, int receptionTicks, PopulationLocation? location,
            PopulationAssignment assignment, CivilianProfession profession, CivilianTier tier, int workXp,
            string qualification, int combatXp, PopulationEquipment equipment, string? captor = null, PopulationLocation? home = null)
        {
            PopulationText.Status(status); PopulationText.Profession(profession); PopulationText.Tier(tier);
            if (id < 0 || receptionTicks < 0 || workXp < 0 || combatXp < 0 || qualification == null)
                throw new ArgumentException("Identity, progress and XP must be nonnegative; qualification must be explicit.");
            PopulationText.ValidateText(qualification); PopulationText.ValidateText(captor);
            Id = id; Status = status; ReceptionTicks = receptionTicks; Location = location;
            Assignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            Profession = profession; Tier = tier; WorkXp = workXp; Qualification = qualification; CombatXp = combatXp;
            Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment)); Captor = captor; Home = home;
        }
        internal PopulationUnit WithId(int id) => new PopulationUnit(id, Status, ReceptionTicks, Location, Assignment,
            Profession, Tier, WorkXp, Qualification, CombatXp, Equipment, Captor, Home);
        public PopulationUnit WithReception(int ticks) => new PopulationUnit(Id, Status, ticks, Location, Assignment,
            Profession, Tier, WorkXp, Qualification, CombatXp, Equipment, Captor, Home);
        internal PopulationUnit WithPlacement(PopulationStatus status, PopulationLocation? location, PopulationAssignment assignment,
            string? captor, PopulationLocation? home) => new PopulationUnit(Id, status, ReceptionTicks, location, assignment,
                Profession, Tier, WorkXp, Qualification, CombatXp, Equipment, captor, home);
        internal string Canonical() => PopulationText.Fields(Id, PopulationText.Status(Status), ReceptionTicks,
            Location?.Canonical(), Assignment.Canonical(), PopulationText.Profession(Profession), PopulationText.Tier(Tier),
            WorkXp, Qualification, CombatXp, Equipment.Canonical(), Captor, Home?.Canonical());
    }

    internal static class PopulationText
    {
        // Length prefixes distinguish delimiters/null; well-formed UTF-16 ensures UTF-8 hash encoding is lossless.
        internal static void ValidateText(string? value)
        {
            if (value == null) return;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsHighSurrogate(value[i]))
                {
                    if (++i >= value.Length || !char.IsLowSurrogate(value[i]))
                        throw new ArgumentException("Population references require well-formed UTF-16.", nameof(value));
                }
                else if (char.IsLowSurrogate(value[i]))
                    throw new ArgumentException("Population references require well-formed UTF-16.", nameof(value));
            }
        }
        internal static string Fields(params object?[] fields) => string.Join("|", fields.Select(Field));
        static string Field(object? field)
        {
            if (field == null) return "-";
            var value = field is bool b ? (b ? "true" : "false") : Convert.ToString(field, CultureInfo.InvariantCulture)!;
            ValidateText(value);
            return value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
        }
        internal static string Status(PopulationStatus x) => x switch {
            PopulationStatus.Refugee => "refugee", PopulationStatus.FreeCivilian => "free_civilian",
            PopulationStatus.CivilianInTransit => "civilian_in_transit", PopulationStatus.Trainee => "trainee",
            PopulationStatus.ActiveSoldier => "active_soldier", PopulationStatus.Captured => "captured",
            _ => throw new ArgumentException("Unknown population status.") };
        internal static string Assignment(PopulationAssignmentKind x) => x switch {
            PopulationAssignmentKind.Unassigned => "unassigned", PopulationAssignmentKind.Work => "work",
            PopulationAssignmentKind.Travel => "travel", PopulationAssignmentKind.Reception => "reception",
            PopulationAssignmentKind.Course => "course", PopulationAssignmentKind.Teaching => "teaching",
            PopulationAssignmentKind.ActiveService => "active_service", _ => throw new ArgumentException("Unknown assignment.") };
        internal static string Profession(CivilianProfession x) => x switch {
            CivilianProfession.None => "none", CivilianProfession.Farmer => "farmer", CivilianProfession.Pioneer => "pioneer",
            CivilianProfession.Craftsman => "craftsman", _ => throw new ArgumentException("Unknown profession.") };
        internal static string Tier(CivilianTier x) => x switch {
            CivilianTier.None => "none", CivilianTier.Novice => "novice", CivilianTier.Trained => "trained",
            CivilianTier.Veteran => "veteran", CivilianTier.Apprentice => "apprentice", CivilianTier.Expert => "expert",
            _ => throw new ArgumentException("Unknown civilian tier.") };
    }
}
