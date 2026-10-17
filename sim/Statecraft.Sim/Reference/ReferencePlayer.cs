using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim.Reference
{
    /// <summary>Test policy for the GDD goldens (D-A7): a stated simple policy, not game AI and not historical. It sends
    /// HistoricalAI's orders, then fills each city's jobs with as many civilians as there are jobs, then sends each idle halutz
    /// unit to the first available yishuv site, or, once none is left, to the lowest free segment of the lowest-id corridor, then, when there is no halutz and none in training, trains one halutz group in
    /// the first city with a Hakhshara and a civilian left without work, while the turn's Attention lasts (both orders borrow). It lives in Statecraft.Sim only so Unity can replay the goldens (#394; GoldenRun); no game mode or UI may reach it
    /// (ReferencePlayerTests.NoGameModeOrUiReachesIt).</summary>
    public static class ReferencePlayer
    {
        public static IReadOnlyList<Command> Commands(Content c, GameState s)
        {
            var list = new List<Command>(HistoricalAI.Commands(c, s));
            var left = s.Attention - list.Sum(x => AttentionSystem.Cost(c, s, x.Kind));
            foreach (var city in s.Cities.Values)
            {
                var want = Math.Min(city.JobSlots, city.Civilians);
                if (want == city.Employed) continue;
                var cost = AttentionSystem.Cost(c, s, Cities.Kind);
                if (cost > left + 1e-9) continue;   // AttentionSystem.TrySpend's rule
                list.Add(new AssignWorkers(city.Id, "job", want));
                left -= cost;
            }
            var halutz = c.UnitTypes.FirstOrDefault(t => t.Id == "halutz");
            if (halutz == null) return list;
            Build(c, s, list, ref left);
            if (s.Units.Values.Any(u => Projects.IsPioneer(u.Type)) || s.Cohorts.Values.Any(k => k.Type == halutz.Id)) return list;
            var trainCost = AttentionSystem.Cost(c, s, Training.TrainKind);
            if (trainCost > left + 1e-9) return list;   // training borrows, so the Treasury does not hold it back
            foreach (var city in s.Cities.Values)
            {
                var employed = list.OfType<AssignWorkers>().Where(a => a.City == city.Id).Select(a => a.Count).DefaultIfEmpty(city.Employed).Last();
                if (city.Civilians - employed < 1 || !c.Cities.First(x => x.Id == city.Id).Facilities.Contains(halutz.Facility)) continue;
                list.Add(new Train(city.Id, halutz.Id, 1));
                break;
            }
            return list;
        }

        /// <summary>Sends each idle halutz unit, veterans too, in id order, to the first yishuv site without a record, in ordinal id order, or,
        /// once every yishuv site has one, to the lowest unbuilt, unclaimed segment of the lowest-id corridor (ordinal), with the
        /// action its kind takes (Phase 4 PR 2, #786), while the turn's Attention lasts. Pioneer work borrows, so the Treasury
        /// does not hold it back.</summary>
        static void Build(Content c, GameState s, List<Command> list, ref double left)
        {
            var cost = AttentionSystem.Cost(c, s, Projects.PioneerKind);
            var taken = new HashSet<string>(s.Sites.Keys, StringComparer.Ordinal);
            foreach (var u in s.Units.Values.Where(u => Projects.IsPioneer(u.Type) && !Projects.IsBusy(s, u.Id)))
            {
                if (cost > left + 1e-9) return;
                var site = c.Sites.Where(x => x.Kinds.Contains(Projects.YishuvKind) && !taken.Contains(x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal).FirstOrDefault();
                if (site != null)
                {
                    list.Add(new PioneerWork(u.Id, Projects.BuildYishuv, site.Id));
                    taken.Add(site.Id);
                }
                else
                {
                    var segment = FreeSegment(c, s, taken);
                    if (segment == null) return;
                    list.Add(new PioneerWork(u.Id, segment.Value.Action, segment.Value.Target));
                    taken.Add(segment.Value.Target);
                }
                left -= cost;
            }
        }

        /// <summary>The lowest segment, by ordinal corridor id then index, that has no record and is not in
        /// <paramref name="taken"/> (this turn's orders), as its action and "corridor:N" target; null when there is none.</summary>
        static (string Action, string Target)? FreeSegment(Content c, GameState s, HashSet<string> taken)
        {
            foreach (var d in c.Corridors.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                s.Corridors.TryGetValue(d.Id, out var w);
                for (int i = 0; i < d.Segments.Count; i++)
                {
                    var target = d.Id + ":" + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (w?.Segments[i] == null && !taken.Contains(target))
                        return (d.Kind == Corridors.Rail ? Corridors.BuildRail : Corridors.BuildRoad, target);
                }
            }
            return null;
        }
    }
}
