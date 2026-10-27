using System;

namespace Statecraft.Sim
{
    /// <summary>Immutable catalogue contract for #1699; data only, never a live-loop switch.
    /// Source: content/gdd03-balance.yaml:307-354 (#1668 balance.md:129-137).
    /// No commands, state mutations, combat terms or inferred defaults.</summary>
    public sealed class Gdd03MilitaryCatalogue
    {
        public int CampCapacity { get; }
        public int BasicTurns { get; }
        public int IssueArms { get; }
        public decimal DraftMoney { get; }
        public int DraftWait { get; }
        public decimal DemobiliseMoney { get; }
        public int DemobiliseWait { get; }
        public int DemobiliseReturnArms { get; }
        public int RepeatIssueArms { get; }
        public decimal TrainingMoney { get; }
        public decimal ActiveMoney { get; }

        Gdd03MilitaryCatalogue(Gdd03BalanceCatalogue catalogue)
        {
            CampCapacity = ReadCampCapacity(catalogue);
            BasicTurns = Integer(catalogue, "basic_turns", true);
            IssueArms = Integer(catalogue, "issue_arms", true);
            DraftMoney = Scalar(catalogue, "draft_money");
            DraftWait = Integer(catalogue, "draft_wait");
            DemobiliseMoney = Scalar(catalogue, "demobilise_money");
            DemobiliseWait = Integer(catalogue, "demobilise_wait");
            DemobiliseReturnArms = Integer(catalogue, "demobilise_return", true);
            RepeatIssueArms = Integer(catalogue, "repeat_issue");
            TrainingMoney = Scalar(catalogue, "training_money");
            ActiveMoney = Scalar(catalogue, "active_money");
        }

        /// <summary>Explicit opt-in data read. Deferred/unsettled terms refuse construction;
        /// their source rows remain untouched. Does not select a gameplay ruleset.</summary>
        public static Gdd03MilitaryCatalogue Read(Gdd03BalanceCatalogue catalogue)
        {
            RequireCatalogue(catalogue);
            return new Gdd03MilitaryCatalogue(catalogue);
        }

        /// <summary>Reads only military.camp_capacity: a positive Agreed Int32 scalar with
        /// provenance. Other military terms are not required. Does not enable camp commands.</summary>
        public static int ReadCampCapacity(Gdd03BalanceCatalogue catalogue)
        {
            RequireCatalogue(catalogue);
            return Integer(catalogue, "camp_capacity", true);
        }

        static void RequireCatalogue(Gdd03BalanceCatalogue catalogue)
        {
            if (catalogue == null) throw new ArgumentNullException(nameof(catalogue));
            if (catalogue.Ruleset != SaveGame.Gdd03Ruleset || catalogue.Values == null)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "ruleset", "requires the gdd-0.3 catalogue; no old-rules fallback");
        }

        static decimal Scalar(Gdd03BalanceCatalogue catalogue, string suffix)
        {
            string key = "military." + suffix;
            if (!catalogue.Values.TryGetValue(key, out var row) || row == null || row.Basis != "Agreed"
                || row.Values == null || row.Values.Count != 1 || row.Values[0] < 0
                || !string.IsNullOrEmpty(row.Expression) || row.Parts == null || row.Parts.Count != 0
                || string.IsNullOrWhiteSpace(row.Note))
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values." + key,
                    "requires one nonnegative Agreed scalar; Deferred remains unavailable, without a fallback");
            return row.Values[0];
        }

        static int Integer(Gdd03BalanceCatalogue catalogue, string suffix, bool positive = false)
        {
            decimal value = Scalar(catalogue, suffix);
            if (value != decimal.Truncate(value) || value > int.MaxValue || (positive && value == 0))
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.military." + suffix,
                    "requires an exact in-range integer (positive for capacity, duration and equipment)");
            return (int)value;
        }
    }
}
