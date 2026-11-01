using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    internal enum Gdd03BuilderTier { OrdinaryOrNovice, Apprentice, Expert }

    /// <summary>One synthetic city project record: work done instead of turns left (#1700).</summary>
    internal sealed class Gdd03FixtureProject
    {
        internal int Id { get; }
        internal Gdd03ProjectCatalogue.Project Kind { get; }
        internal decimal WorkDone { get; set; }
        internal int? CompletedTurn { get; set; }
        internal Gdd03FixtureProject(int id, Gdd03ProjectCatalogue.Project kind) { Id = id; Kind = kind; }
    }

    /// <summary>Builder rules over the #1963 catalogue (#1700), synthetic fixtures only: no command, save, loader,
    /// resolver or UI caller, so the game keeps R-PROJ-001..003 and its IL 0m upkeep (R-MONEY-002).</summary>
    internal sealed class Gdd03ProjectBuildFixture
    {
        readonly Gdd03ProjectCatalogue catalogue;
        readonly List<Gdd03FixtureProject> projects = new List<Gdd03FixtureProject>();
        internal decimal Money { get; private set; }
        internal int Turn { get; private set; }
        internal IReadOnlyList<Gdd03FixtureProject> Projects => projects;

        internal Gdd03ProjectBuildFixture(Gdd03ProjectCatalogue catalogue, decimal money)
        {
            this.catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            Money = money;
        }

        /// <summary>Pays the kind's full cost up front; refused, nothing changed, when Money is short. The national
        /// start allowance, cancel and housing repair are other #1700 slices.</summary>
        internal int Start(string key)
        {
            var kind = catalogue.Projects.SingleOrDefault(p => p.Key == key)
                ?? throw new InvalidOperationException("Unknown project kind: " + key);
            if (Money < kind.Cost.Amount) throw new InvalidOperationException("Not enough Money for " + key);
            Money -= kind.Cost.Amount;
            var project = new Gdd03FixtureProject(projects.Count + 1, kind);
            projects.Add(project);
            return project.Id;
        }

        /// <summary>Resolves one turn. Only assigned builders add work, at their tier's rate, at most the catalogue's
        /// slots per project; Money is not an input, so extra Money cannot speed a project. Excess work is discarded.
        /// Then every completed project, the one completed this turn included and vacant or not, is charged its upkeep.
        /// Unpaid upkeep is #1702's settlement, so Money may go below 0 here.</summary>
        internal void EndTurn(IReadOnlyDictionary<int, Gdd03BuilderTier[]> builders)
        {
            // Validate everything first so a refused turn changes nothing.
            foreach (var entry in builders)
            {
                var p = projects.SingleOrDefault(x => x.Id == entry.Key)
                    ?? throw new InvalidOperationException("Unknown project " + entry.Key);
                if (p.CompletedTurn != null) throw new InvalidOperationException("Project " + entry.Key + " is complete");
                if (entry.Value.Length > catalogue.BuilderSlots.Amount)
                    throw new InvalidOperationException("Project " + entry.Key + " has " + catalogue.BuilderSlots.Amount + " builder slots");
            }
            Turn++;
            foreach (var p in projects)
            {
                if (p.CompletedTurn != null || !builders.TryGetValue(p.Id, out var tiers)) continue;
                p.WorkDone = Math.Min(p.Kind.Work.Amount, p.WorkDone + tiers.Sum(Rate));
                if (p.WorkDone == p.Kind.Work.Amount) p.CompletedTurn = Turn;
            }
            foreach (var p in projects)
                if (p.CompletedTurn != null) Money -= p.Kind.Upkeep.Amount;
        }

        decimal Rate(Gdd03BuilderTier tier) => tier switch
        {
            Gdd03BuilderTier.Expert => catalogue.ExpertRate.Amount,
            Gdd03BuilderTier.Apprentice => catalogue.ApprenticeRate.Amount,
            _ => catalogue.OrdinaryOrNoviceRate.Amount
        };
    }
}
