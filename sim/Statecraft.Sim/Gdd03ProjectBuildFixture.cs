using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>One synthetic city project record: the turns its builder has worked, against the kind's turns (#1700).</summary>
    internal sealed class Gdd03FixtureProject
    {
        internal int Id { get; }
        internal Gdd03ProjectCatalogue.Project Kind { get; }
        /// <summary>The kind's `turns` in content/projects.yaml, the live game's length (owner, 3 November 2026, #1700).</summary>
        internal int Turns { get; }
        internal int TurnsDone { get; set; }
        internal int? CompletedTurn { get; set; }
        internal Gdd03FixtureProject(int id, Gdd03ProjectCatalogue.Project kind, int turns) { Id = id; Kind = kind; Turns = turns; }
    }

    /// <summary>Builder rules over the #1963 catalogue (#1700), synthetic fixtures only: no command, save, loader,
    /// resolver or UI caller, so the game keeps R-PROJ-001..003 and its IL 0m upkeep (R-MONEY-002).</summary>
    internal sealed class Gdd03ProjectBuildFixture
    {
        // R-MONEY-002's MVP rate for a completed project (owner, 30 October 2026 "Keep ruling", kept on #1700 on
        // 1 November). The catalogue's non-zero Upkeep is post-MVP, so it is not read here.
        const decimal MvpUpkeep = 0m;
        readonly Gdd03ProjectCatalogue catalogue;
        readonly IReadOnlyList<ProjectKind> kinds;
        readonly List<Gdd03FixtureProject> projects = new List<Gdd03FixtureProject>();
        // #1700 slice B (owner, 3 November 2026: "One per city, plus 2 nationally"): each city's starts this turn.
        readonly Dictionary<string, int> startsThisTurn = new Dictionary<string, int>(StringComparer.Ordinal);
        internal decimal Money { get; private set; }
        internal decimal Debt { get; private set; }
        internal int Turn { get; private set; }
        internal IReadOnlyList<Gdd03FixtureProject> Projects => projects;

        /// <summary>The national limit on city starts a turn: the game's map_actions_per_turn (content/rules.yaml), read
        /// from <paramref name="rules"/>, which the owner's 3 November 2026 ruling keeps ("One per city, plus 2 nationally").</summary>
        internal int NationalStarts { get; }

        /// <param name="kinds">The live project kinds (content/projects.yaml), read for each kind's `turns` only.</param>
        internal Gdd03ProjectBuildFixture(Gdd03ProjectCatalogue catalogue, Rules rules, IReadOnlyList<ProjectKind> kinds, decimal money)
        {
            this.catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            this.kinds = kinds ?? throw new ArgumentNullException(nameof(kinds));
            NationalStarts = (rules ?? throw new ArgumentNullException(nameof(rules))).MapActionsPerTurn;
            Money = money;
        }

        /// <summary>Pays the kind's full cost up front, borrowing any shortfall (D1, R-MONEY-003). A city starts at most
        /// <see cref="Statecraft.Sim.Projects.Gdd03CityStartsPerTurn"/> (1) project a turn, and <see cref="NationalStarts"/> (map_actions_per_turn) starts a turn
        /// nationally, as R-PROJ-001 under gdd-0.3; a start past either is refused before anything is paid, the city's
        /// limit first. A catalogue kind with no row of the same id in content/projects.yaml has no `turns` to count, so it is
        /// refused before anything is paid (maintainer reading). Cancel and housing repair are other #1700 slices.</summary>
        internal int Start(string key, string city)
        {
            var kind = catalogue.Projects.SingleOrDefault(p => p.Key == key)
                ?? throw new InvalidOperationException("Unknown project kind: " + key);
            var turns = kinds.SingleOrDefault(k => k.Id == key)?.Turns
                ?? throw new InvalidOperationException("No turns in projects.yaml for project kind: " + key);
            var inCity = startsThisTurn.TryGetValue(city, out var n) ? n : 0;
            if (inCity >= Statecraft.Sim.Projects.Gdd03CityStartsPerTurn)
                throw new InvalidOperationException(city + " has started a city project this turn");
            if (startsThisTurn.Values.Sum() >= NationalStarts)
                throw new InvalidOperationException("No more than " + NationalStarts + " city starts are allowed a turn");
            Pay(kind.Cost.Amount);
            var project = new Gdd03FixtureProject(projects.Count + 1, kind, turns);
            projects.Add(project);
            startsThisTurn[city] = inCity + 1;
            return project.Id;
        }

        /// <summary>Resolves one turn. <paramref name="built"/> lists the projects that have their one builder this turn
        /// (owner, 3 November 2026, #1700: "One builder, today's times (Recommended)"); each advances one of its kind's
        /// turns, and a project not listed waits. Money is not an input, so extra Money cannot speed a project. Then every
        /// completed project, the one completed this turn included and vacant or not, is charged the MVP upkeep rate, IL 0m.</summary>
        internal void EndTurn(IReadOnlyCollection<int> built)
        {
            // Validate everything first so a refused turn changes nothing; id order makes the first error deterministic.
            foreach (var id in built.OrderBy(i => i))
            {
                var p = projects.SingleOrDefault(x => x.Id == id)
                    ?? throw new InvalidOperationException("Unknown project " + id);
                if (p.CompletedTurn != null) throw new InvalidOperationException("Project " + id + " is complete");
            }
            if (built.Distinct().Count() != built.Count) throw new InvalidOperationException("A project has one builder");
            Turn++;
            startsThisTurn.Clear();
            foreach (var p in projects)
            {
                if (p.CompletedTurn != null || !built.Contains(p.Id)) continue;
                p.TurnsDone++;
                if (p.TurnsDone == p.Turns) p.CompletedTurn = Turn;
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
    }
}
