using System;
using System.Collections.Generic;

namespace Statecraft.Sim
{
    // Internal until producer integration is approved. Evidence must describe completed work, not intent.
    internal enum CivilianWorkKind { GenericWork, StaffedFarming, PioneerAdvancement }

    internal sealed class CivilianWorkEvidence
    {
        internal int PopulationId { get; }
        internal string Target { get; }
        internal CivilianWorkKind Kind { get; }
        internal decimal Advancement { get; }
        internal CivilianWorkEvidence(int populationId, string target, CivilianWorkKind kind, decimal advancement)
        {
            if (populationId < 0) throw new ArgumentOutOfRangeException(nameof(populationId));
            if (advancement < 0) throw new ArgumentOutOfRangeException(nameof(advancement));
            if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("Work needs its explicit assignment target.");
            PopulationText.ValidateText(target);
            if (kind != CivilianWorkKind.GenericWork && kind != CivilianWorkKind.StaffedFarming && kind != CivilianWorkKind.PioneerAdvancement)
                throw new ArgumentException("Unknown civilian work kind.");
            PopulationId = populationId; Target = target; Kind = kind; Advancement = advancement;
        }
    }

    /// <summary>Pure region-independent fixture rules. No caller in the game, payment, assignment or resolver.
    /// Outputs are proposed immutable replacements, not mutations or replay commands. One evaluation uses the
    /// turn-start ledger and ALL completed-work evidence for one id. The future resolver must commit once at
    /// turn end; feeding a returned record back within the same turn would incorrectly credit a second turn.</summary>
    internal static class CivilianProfessionRules
    {
        static void RequireFixture(Content c, GameState s)
        {
            if (!c.UsesPopulationLedger || !s.UsesPopulationLedger || !s.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Civilian profession helpers require an initialized gdd-0.3 fixture.");
        }
        static bool FarmerOrPioneer(CivilianProfession profession)
            => profession == CivilianProfession.Farmer || profession == CivilianProfession.Pioneer;
        static PopulationUnit WithTierAndXp(PopulationUnit p, CivilianTier tier, int xp)
            => new PopulationUnit(p.Id, p.Status, p.ReceptionTicks, p.Location, p.Assignment,
                p.Profession, tier, xp, p.Qualification, p.CombatXp, p.Equipment, p.Captor, p.Home);

        // R-VET-001: at most one XP increment in this pure evaluation, irrespective of evidence count/order.
        internal static PopulationUnit AfterWork(Content c, GameState turnStart, int id, IEnumerable<CivilianWorkEvidence> evidence)
        {
            RequireFixture(c, turnStart);
            var p = turnStart.PopulationLedger.Get(id);
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));
            bool worked = false;
            foreach (var e in evidence)
            {
                if (e == null || e.PopulationId != id) throw new ArgumentException("Evidence must name the evaluated population id.");
                bool matching = (p.Profession == CivilianProfession.Farmer && e.Kind == CivilianWorkKind.StaffedFarming)
                    || (p.Profession == CivilianProfession.Pioneer && e.Kind == CivilianWorkKind.PioneerAdvancement);
                // R-VET-001: completed staffed farming counts regardless of output; pioneers must advance a project.
                if (matching && e.Target == p.Assignment.Target
                    && (e.Kind == CivilianWorkKind.StaffedFarming || e.Advancement > 0)) worked = true;
            }
            if (!worked || !FarmerOrPioneer(p.Profession) || p.Status != PopulationStatus.FreeCivilian
                || p.Assignment.Kind != PopulationAssignmentKind.Work
                || p.ReceptionTicks != c.RequirePopulationInteger("reception.turns")) return p;
            int threshold = c.RequirePopulationInteger("profession.work_to_veteran");
            int xp = checked(p.WorkXp + 1);
            var tier = xp >= threshold ? CivilianTier.Veteran : p.Tier;
            return WithTierAndXp(p, tier, xp);
        }

        // R-VET-002: always read the snapshot tier, never the proposed end-of-turn replacement.
        internal static decimal OutputAtTurnStart(Content c, GameState turnStart, int id)
        {
            RequireFixture(c, turnStart);
            var p = turnStart.PopulationLedger.Get(id);
            if (!FarmerOrPioneer(p.Profession)) throw new ArgumentException("Only farmer/pioneer output is defined here.");
            string key = p.Profession == CivilianProfession.Farmer ? "farmer.output" : "pioneer.work_rate";
            var catalogue = c.Gdd03Balance;
            if (catalogue == null || catalogue.Ruleset != SaveGame.Gdd03Ruleset || catalogue.Values == null
                || !catalogue.Values.TryGetValue(key, out var row) || row == null || row.Basis != "Agreed"
                || row.Values == null || row.Values.Count != 3 || row.Parts == null || row.Parts.Count != 0
                || !string.IsNullOrEmpty(row.Expression) || row.Values.Exists(x => x <= 0))
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values." + key,
                    "requires positive Agreed novice/trained/veteran values; no fallback");
            int index = p.Tier switch {
                CivilianTier.Novice => 0, CivilianTier.Trained => 1, CivilianTier.Veteran => 2,
                _ => throw new ArgumentException("Invalid farmer/pioneer tier.") };
            return row.Values[index];
        }

        // R-QUAL-008: qualification transform only. Course eligibility/completion, fees and release are NOT inferred.
        internal static PopulationUnit AfterMatchingCourse(Content c, GameState s, int id, CivilianProfession courseProfession)
        {
            RequireFixture(c, s);
            var p = s.PopulationLedger.Get(id);
            if (!FarmerOrPioneer(courseProfession) || p.Profession != courseProfession
                || p.Status != PopulationStatus.Trainee || p.Assignment.Kind != PopulationAssignmentKind.Course)
                throw new ArgumentException("Only a matching farmer/pioneer course can propose a trained tier.");
            return p.Tier == CivilianTier.Veteran || p.Tier == CivilianTier.Trained
                ? p : WithTierAndXp(p, CivilianTier.Trained, p.WorkXp);
        }

        // R-QUAL-008: quote ONLY. #1702 owns affordability/credit; no acquisition or payment is performed.
        internal static int ToolsPrice(Content c, GameState s, CivilianProfession profession)
        {
            RequireFixture(c, s);
            string key = profession switch {
                CivilianProfession.Farmer => "profession.farmer_tools",
                CivilianProfession.Pioneer => "profession.pioneer_tools",
                _ => throw new ArgumentException("No farmer/pioneer tools price for this profession.") };
            return c.RequirePopulationInteger(key);
        }
    }
}
