using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim.Reference
{
    /// <summary>Test policy for the GDD goldens (D-A7): a stated simple policy, not game AI and not historical. It sends
    /// HistoricalAI's orders and no AssignWorkers (R-CITY-009's default fills the jobs, and an order would turn it off), then
    /// has each of Israel's units with no attack, in id order, attack the first neighbouring hex (in the fixed order) whose
    /// enemy units it meets at <see cref="AttackRatioPercent"/> percent or better by the combat preview, while the Arms last
    /// for every standing attack and the new one (R-COMBAT-001, R-COMBAT-004), then,
    /// when no city project is being built, starts a housing project in the city with the most overflow that has a hex for it, then sends each idle halutz
    /// unit to the first available yishuv site (then to the first ready field, then to the first available camp site, then to the lowest free segment of the lowest-id corridor), then,
    /// when there is no halutz and none in training, trains one halutz group in
    /// the first city with a Hakhshara and a civilian left without work, while the turn's Attention lasts (both orders borrow). It lives in Statecraft.Sim only so Unity can replay the goldens (#394; GoldenRun); no game mode or UI may reach it
    /// (ReferencePlayerTests.NoGameModeOrUiReachesIt).</summary>
    public static class ReferencePlayer
    {
        /// <summary>The attack policy's threshold: 3:2 (#747; test code, not a design value).</summary>
        public const int AttackRatioPercent = 150;

        public static IReadOnlyList<Command> Commands(Content c, GameState s)
        {
            var list = new List<Command>(HistoricalAI.Commands(c, s));
            var left = s.Attention - list.Sum(x => AttentionSystem.Cost(c, s, x.Kind));
            Attack(c, s, list, ref left);
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

        /// <summary>The attack policy in the class comment. Unit orders are free (D-A6), but the cost is still checked.</summary>
        static void Attack(Content c, GameState s, List<Command> list, ref double left)
        {
            if (!c.IsGdd) return;
            var cost = AttentionSystem.Cost(c, s, Orders.Kind);
            var per = c.RulesAt(s.Date).Mvp!.ArmsPerAttack!.Value;
            var attacking = s.Units.Values.Count(u => u.Side == Movement.PlayerSide && u.Order == Combat.Attack);
            foreach (var u in s.Units.Values.Where(u => u.Side == Movement.PlayerSide && u.Order != Combat.Attack))
            {
                if (cost > left + 1e-9 || s.Arms < per * (attacking + 1)) return;
                foreach (var (dq, dr) in Movement.Neighbours)
                {
                    var preview = Combat.Preview(c, s, u.Id, u.Q + dq, u.R + dr);
                    if (preview == null || preview.Value.RatioPercent < AttackRatioPercent) continue;
                    list.Add(new UnitOrder(u.Id, Combat.Attack, u.Q + dq, u.R + dr));
                    left -= cost;
                    attacking++;
                    break;
                }
            }
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
        /// is none, to build the first camp site without a record (R-CAMP-001), and when there is none, to the lowest unbuilt,
        /// unclaimed segment of the lowest-id corridor (ordinal), with the action its kind takes (R-COR-001), while the turn's
        /// Attention lasts. Pioneer work borrows, so the Treasury does not hold it back.</summary>
        static void Build(Content c, GameState s, List<Command> list, ref double left)
        {
            var cost = AttentionSystem.Cost(c, s, Projects.PioneerKind);
            var taken = new HashSet<string>(s.Sites.Keys, StringComparer.Ordinal);
            foreach (var u in s.Units.Values.Where(u => Projects.IsPioneer(u.Type) && !Projects.IsBusy(s, u.Id)))
            {
                if (cost > left + 1e-9) return;
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
                if (site != null)
                {
                    list.Add(new PioneerWork(u.Id, action, site.Id));
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
