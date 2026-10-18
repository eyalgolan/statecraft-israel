using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim.Reference
{
    /// <summary>Test policy for the GDD goldens (D-A7): a stated simple policy, not game AI and not historical. It sends
    /// HistoricalAI's orders and no AssignWorkers (R-CITY-009's default fills the jobs, and an order would turn it off), then,
    /// when no city project is being built, starts a housing project in the city with the most overflow that has a hex for it, then sends each idle halutz
    /// unit to the first available yishuv site (then to the first ready field, then to the first available camp site), then,
    /// when there is no halutz and none in training, trains one halutz group in
    /// the first city with a Hakhshara and a civilian left without work, while the turn's Attention lasts (both orders borrow). It lives in Statecraft.Sim only so Unity can replay the goldens (#394; GoldenRun); no game mode or UI may reach it
    /// (ReferencePlayerTests.NoGameModeOrUiReachesIt).</summary>
    public static class ReferencePlayer
    {
        public static IReadOnlyList<Command> Commands(Content c, GameState s)
        {
            var list = new List<Command>(HistoricalAI.Commands(c, s));
            var left = s.Attention - list.Sum(x => AttentionSystem.Cost(c, s, x.Kind));
            House(c, s, list, ref left);
            var halutz = c.UnitTypes.FirstOrDefault(t => t.Id == "halutz");
            if (halutz == null) return list;
            Build(c, s, list, ref left);
            if (s.Units.Values.Any(u => Projects.IsPioneer(u.Type)) || s.Cohorts.Values.Any(k => k.Type == halutz.Id)) return list;
            var trainCost = AttentionSystem.Cost(c, s, Training.TrainKind);
            if (trainCost > left + 1e-9) return list;   // training borrows, so the Treasury does not hold it back
            foreach (var city in s.Cities.Values)
            {
                if (Training.Idle(city) < 1 || !Training.HasFacility(c, s, city.Id, halutz.Facility)) continue;
                list.Add(new Train(city.Id, halutz.Id, 1));
                break;
            }
            return list;
        }

        /// <summary>Starts a housing project (R-PROJ-001) when none is being built, in the city with the most overflow (Cities.Overflow; the first
        /// in ordinal id order on a tie) that has a hex left for it, on the first hex in (q, r) order that Projects.CheckCity accepts, when the turn's
        /// Attention lasts. It borrows, as the other orders do.</summary>
        static void House(Content c, GameState s, List<Command> list, ref double left)
        {
            var kind = c.ProjectKinds.FirstOrDefault(k => k.Id == MapActions.Housing);
            // One at a time: the project borrows, and a start every turn would run the debt far past the run's plausibility band
            // (golden/invariants.yaml debt-grows-through-the-run).
            if (kind == null || s.CityProjects.Values.Any(p => p.BuiltOn == null)) return;   // the legacy loop has no city projects
            var cost = AttentionSystem.Cost(c, s, Projects.StartProjectKind);
            if (cost > left + 1e-9) return;   // a city project borrows (Q-MONEY-3), so the Treasury does not hold it back
            var max = c.RulesAt(s.Date).MapActionMaxDistance;
            // OrderBy is stable, so a tie keeps the cities' ordinal id order.
            foreach (var city in s.Cities.Values.Where(x => Cities.Overflow(s, x) > 0).OrderByDescending(x => Cities.Overflow(s, x)))
            {
                var home = c.SettlementHexes.First(h => h.Id == city.Id);
                for (int q = home.Q - max; q <= home.Q + max; q++)
                    for (int r = home.R - max; r <= home.R + max; r++)
                        if (Projects.CheckCity(c, s, kind.Id, city.Id, q, r) == null)
                        {
                            list.Add(new StartProject(kind.Id, city.Id, q, r));
                            left -= cost;
                            return;
                        }
            }
        }

        /// <summary>Sends each idle halutz unit, veterans too, in id order, to the first yishuv site without a record, in ordinal id order,
        /// and once every yishuv site has one, to plow the first ready field site without a record (R-FIELD-001), and when there
        /// is none, to build the first camp site without a record (R-CAMP-001), while the turn's Attention lasts. Pioneer work
        /// borrows, so the Treasury does not hold it back.</summary>
        static void Build(Content c, GameState s, List<Command> list, ref double left)
        {
            var cost = AttentionSystem.Cost(c, s, Projects.PioneerKind);
            var taken = new HashSet<string>(s.Sites.Keys, StringComparer.Ordinal);
            foreach (var u in s.Units.Values.Where(u => Projects.IsPioneer(u.Type) && !Projects.IsBusy(s, u.Id)))
            {
                var site = c.Sites.Where(x => x.Kinds.Contains(Projects.YishuvKind) && !taken.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault();
                var action = Projects.BuildYishuv;
                if (site == null)
                {
                    site = c.Sites.Where(x => x.Kinds.Contains(Projects.FieldKind) && !taken.Contains(x.Id) && Projects.YishuvToBuildFirst(c, s, x) == null)
                        .OrderBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault();
                    action = Projects.PlowFields;
                }
                if (site == null)
                {
                    site = c.Sites.Where(x => Projects.IsCamp(x) && !taken.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault();
                    action = Projects.BuildCamp;
                }
                if (site == null || cost > left + 1e-9) return;
                list.Add(new PioneerWork(u.Id, action, site.Id));
                taken.Add(site.Id);
                left -= cost;
            }
        }
    }
}
