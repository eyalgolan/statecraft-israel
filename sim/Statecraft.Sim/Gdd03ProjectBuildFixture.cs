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
        // R-MONEY-002's MVP rate for a completed project (owner, 30 October 2026 "Keep ruling", kept on #1700 on
        // 1 November). The catalogue's non-zero Upkeep is post-MVP, so it is not read here.
        const decimal MvpUpkeep = 0m;
        readonly Gdd03ProjectCatalogue catalogue;
        readonly List<Gdd03FixtureProject> projects = new List<Gdd03FixtureProject>();
        internal decimal Money { get; private set; }
        internal decimal Debt { get; private set; }
        internal int Turn { get; private set; }
        internal IReadOnlyList<Gdd03FixtureProject> Projects => projects;

        internal Gdd03ProjectBuildFixture(Gdd03ProjectCatalogue catalogue, decimal money)
        {
            this.catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            Money = money;
        }

        /// <summary>Pays the kind's full cost up front, borrowing any shortfall (D1, R-MONEY-003). The national
        /// start allowance, cancel and housing repair are other #1700 slices.</summary>
        internal int Start(string key)
        {
            var kind = catalogue.Projects.SingleOrDefault(p => p.Key == key)
                ?? throw new InvalidOperationException("Unknown project kind: " + key);
            Pay(kind.Cost.Amount);
            var project = new Gdd03FixtureProject(projects.Count + 1, kind);
            projects.Add(project);
            return project.Id;
        }

        /// <summary>Resolves one turn. Only assigned builders add work, at their tier's rate, at most the catalogue's
        /// slots per project; Money is not an input, so extra Money cannot speed a project. Excess work is discarded.
        /// Then every completed project, the one completed this turn included and vacant or not, is charged the MVP
        /// upkeep rate, IL 0m.</summary>
        internal void EndTurn(IReadOnlyDictionary<int, Gdd03BuilderTier[]> builders)
        {
            // Validate everything first so a refused turn changes nothing; key order makes the first error deterministic.
            foreach (var entry in builders.OrderBy(e => e.Key))
            {
                var p = projects.SingleOrDefault(x => x.Id == entry.Key)
                    ?? throw new InvalidOperationException("Unknown project " + entry.Key);
                if (p.CompletedTurn != null) throw new InvalidOperationException("Project " + entry.Key + " is complete");
                if (entry.Value.Length > catalogue.BuilderSlots.Amount)
                    throw new InvalidOperationException("Project " + entry.Key + " has " + catalogue.BuilderSlots.Amount + " builder slots");
                foreach (var tier in entry.Value)
                    if (!Enum.IsDefined(typeof(Gdd03BuilderTier), tier)) throw new InvalidOperationException("Unknown builder tier " + (int)tier);
            }
            Turn++;
            foreach (var p in projects)
            {
                if (p.CompletedTurn != null || !builders.TryGetValue(p.Id, out var tiers)) continue;
                p.WorkDone = Math.Min(p.Kind.Work.Amount, p.WorkDone + tiers.Sum(Rate));
                if (p.WorkDone == p.Kind.Work.Amount) p.CompletedTurn = Turn;
            }
            foreach (var p in projects)
                if (p.CompletedTurn != null) Pay(MvpUpkeep);
        }

        // R-MONEY-003: a cost above Money is paid, Money stops at 0 and the shortfall moves to debt.
        void Pay(decimal amount)
        {
            var paid = Math.Min(Money, amount);
            Money -= paid;
            Debt += amount - paid;
        }

        decimal Rate(Gdd03BuilderTier tier) => tier switch
        {
            Gdd03BuilderTier.Expert => catalogue.ExpertRate.Amount,
            Gdd03BuilderTier.Apprentice => catalogue.ApprenticeRate.Amount,
            Gdd03BuilderTier.OrdinaryOrNovice => catalogue.OrdinaryOrNoviceRate.Amount,
            _ => throw new InvalidOperationException("Unknown builder tier " + (int)tier) // refused in validation first
        };
    }
}
