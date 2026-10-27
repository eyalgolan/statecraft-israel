using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Detached, immutable view of settled city-project design data. This is not a
    /// project service or loop switch. Only synthetic callers use it until integration is approved.
    /// Winery, repair pricing and operational construction are deliberately absent.</summary>
    public sealed class Gdd03ProjectCatalogue
    {
        public enum CompletionEffect
        {
            OrdinaryHousing, IndustryJob, ReceptionBeds, MatchingTeacherCourseAndHousing,
            WorkshopWorker, MilitaryTrainees
        }

        public sealed class Number
        {
            public decimal Amount { get; }
            public string Basis { get; }
            public string Note { get; }
            public string SourcePath { get; }
            internal Number(decimal amount, Gdd03DesignValue source, string path)
            { Amount = amount; Basis = source.Basis; Note = source.Note; SourcePath = path; }
        }

        public sealed class Project
        {
            public string Key { get; }
            public string Basis { get; }
            public string Note { get; }
            public Number Cost { get; }
            public Number Work { get; }
            public Number Upkeep { get; }
            public CompletionEffect Effect { get; }
            // Capacity meaning comes from Effect; centres have a qualitative effect, no invented count.
            public Number? Capacity { get; }
            public Number? ConditionPp { get; }
            // Symbolic provenance only; no standalone pioneer operation is implemented.
            public string? StandaloneBasis { get; }
            public string? StandaloneNote { get; }
            internal Project(string key, Gdd03DesignValue row, Number cost, Number work, Number upkeep,
                CompletionEffect effect, Number? capacity, Number? condition, Gdd03DesignValue? standalone)
            {
                Key = key; Basis = row.Basis; Note = row.Note; Cost = cost; Work = work; Upkeep = upkeep;
                Effect = effect; Capacity = capacity; ConditionPp = condition;
                StandaloneBasis = standalone?.Basis; StandaloneNote = standalone?.Note;
            }
        }

        public IReadOnlyList<Project> Projects { get; }
        public Number BuilderSlots { get; }
        public Number CityStarts { get; }
        public Number OrdinaryOrNoviceRate { get; }
        public Number ApprenticeRate { get; }
        public Number ExpertRate { get; }

        private Gdd03ProjectCatalogue(List<Project> projects, Number slots, Number starts, Number[] rates)
        {
            Projects = projects.AsReadOnly(); BuilderSlots = slots; CityStarts = starts;
            OrdinaryOrNoviceRate = rates[0]; ApprenticeRate = rates[1]; ExpertRate = rates[2];
        }

        // R-PROJ-001..003 describe the future consumer; this adapter performs no state mutation.
        public static Gdd03ProjectCatalogue Read(Content content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            // Check the full identity before touching data: IsGdd and catalogue presence are insufficient.
            if (content.Ruleset != SaveGame.Gdd03Ruleset)
                throw new InvalidOperationException("Project catalogue access requires exact gdd-0.3 identity.");
            var catalogue = content.Gdd03Balance;
            if (catalogue == null || catalogue.Ruleset != SaveGame.Gdd03Ruleset || catalogue.Values == null)
                throw Error("values", "requires the gdd-0.3 balance catalogue; no old-project fallback");

            Gdd03DesignValue Root(string key)
            {
                if (!catalogue.Values.TryGetValue(key, out var row) || row == null)
                    throw Error("values." + key, "settled project catalogue row required");
                row.Validate("values." + key);
                return row;
            }
            Number Scalar(string key, bool whole = false) =>
                ReadNumber(Root(key), "values." + key, "Agreed", whole);
            // Shared readers own numeric validation; projects additionally require copied provenance.
            Gdd03DesignValue Provenance(string key)
            {
                if (!catalogue.Values.TryGetValue(key, out var row) || row == null || string.IsNullOrWhiteSpace(row.Note))
                    throw Error("values." + key, "design basis/provenance required");
                return row;
            }
            Number SharedNumber(string key, decimal amount) => new Number(amount, Provenance(key), "values." + key);

            var slots = Scalar("builder.slots_per_project", true);
            var starts = Scalar("construction.starts_per_turn", true);
            var builderRates = Gdd03CraftArithmetic.ReadBuilderRates(catalogue);
            var rateRow = Provenance("builder.work_rate");
            const string ratePath = "values.builder.work_rate";
            // Project-only layer over the shared three nonnegative Agreed rates: positive and ordered.
            if (builderRates.Any(x => x <= 0) || builderRates[0] > builderRates[1] || builderRates[1] > builderRates[2])
                throw Error(ratePath, "requires three positive ordered Agreed ordinary-or-novice/apprentice/expert rates");
            var rates = builderRates.Select(x => new Number(x, rateRow, ratePath)).ToArray();

            Project Descriptor(string key, CompletionEffect effect, string? capacityPart = null,
                Number? capacity = null, bool condition = false, bool standalone = false)
            {
                var row = Root(key); var path = "values." + key;
                var basis = key == "military_camp" ? "Default" : "Agreed";
                var expected = new List<string> { "cost", "work", "upkeep" };
                if (capacityPart != null) expected.Add(capacityPart);
                if (condition) expected.Add("condition_pp");
                if (standalone) expected.Add("standalone_application");
                if (row.Basis != basis || row.Values.Count != 0 || !string.IsNullOrEmpty(row.Expression)
                    || !expected.OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(row.Parts.Keys.OrderBy(x => x, StringComparer.Ordinal)))
                    throw Error(path, "requires the settled labelled project parts; no inferred or extra fields");
                Number Part(string name, string partBasis, bool whole = false, bool pp = false) =>
                    ReadNumber(row.Parts[name], path + ".parts." + name, partBasis, whole, pp);
                Gdd03DesignValue? application = null;
                if (standalone)
                {
                    application = row.Parts["standalone_application"];
                    if (application.Basis != "Default" || application.Values.Count != 0 || application.Parts.Count != 0
                        || application.Expression != "same cost/work")
                        throw Error(path + ".parts.standalone_application", "requires the symbolic Default same cost/work provenance");
                }
                return new Project(key, row, Part("cost", basis), Part("work", basis, true), Part("upkeep", "Default"),
                    effect, capacityPart != null ? Part(capacityPart, "Agreed", true) : capacity,
                    condition ? Part("condition_pp", "Agreed", true, true) : null, application);
            }

            // Source order is explicit and deterministic, never Dictionary iteration order.
            var projects = new List<Project> {
                Descriptor("maabara", CompletionEffect.OrdinaryHousing, "housing", condition: true),
                Descriptor("permanent_housing", CompletionEffect.OrdinaryHousing, "housing", condition: true),
                Descriptor("industry", CompletionEffect.IndustryJob, "job_slots"),
                Descriptor("reception_camp", CompletionEffect.ReceptionBeds,
                    capacity: SharedNumber("reception.capacity", content.RequirePopulationInteger("reception.capacity")), standalone: true),
                Descriptor("training_centre", CompletionEffect.MatchingTeacherCourseAndHousing, standalone: true),
                Descriptor("arms_workshop", CompletionEffect.WorkshopWorker, "worker_slots"),
                Descriptor("military_camp", CompletionEffect.MilitaryTrainees,
                    capacity: SharedNumber("military.camp_capacity", Gdd03MilitaryCatalogue.ReadCampCapacity(catalogue)))
            };
            return new Gdd03ProjectCatalogue(projects, slots, starts, rates);
        }

        private static Number ReadNumber(Gdd03DesignValue row, string path, string basis, bool whole, bool pp = false)
        {
            if (row.Basis != basis || row.Values.Count != 1 || row.Parts.Count != 0 || !string.IsNullOrEmpty(row.Expression))
                throw Error(path, "requires one settled labelled number; Deferred and symbolic quantities are not executable");
            var amount = row.Values[0];
            // Percentage points use the standard [-100,100] unit range, not an initial condition default.
            if (pp ? amount < -100m || amount > 100m : amount <= 0)
                throw Error(path, pp ? "condition effect must be within percentage-point bounds" : "requires a positive quantity");
            if (whole && (decimal.Truncate(amount) != amount || amount > int.MaxValue || amount < int.MinValue))
                throw Error(path, "requires a whole quantity representable as an integer");
            return new Number(amount, row, path);
        }

        private static ContentException Error(string path, string message) =>
            new ContentException(Gdd03BalanceCatalogue.FileName, path, message);
    }
}
