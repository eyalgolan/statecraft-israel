using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Combat in the GDD v0.1 loop (Phase 5 PR 8; docs/rules/mvp.md R-COMBAT-001 to R-COMBAT-006). A unit given
    /// UnitOrder(unit, attack, q, r) attacks the adjacent hex (q, r) in resolver step 2 of every turn until its hex holds no
    /// enemy unit next to it (spec 2026-10-12 section 2.3: a standing order). Units of one side attacking one hex are one
    /// engagement, their strengths summed (Q-COMBAT-2); it draws once from the RNG and reads rules.yaml mvp.combat_table
    /// (Q-COMBAT-1). Combat changes the units and the Arms only (R-COMBAT-005): no resident, city, locality or score is ever
    /// a target or an outcome (Q-CIV-2, impact plan section 4.3).</summary>
    public static class Combat
    {
        public const string Attack = "attack";

        /// <summary>The unit_types.yaml class whose units support attacks from range (R-COMBAT-007; Q-ARTY-1).</summary>
        public const string ArtilleryClass = "artillery";

        /// <summary>RNG values each engagement draws, whatever its outcome (docs/determinism.md, "Combat draws").</summary>
        public const int DrawsPerEngagement = 1;

        /// <summary>The player's team: Israel's units and the halutzim (no side). Every other side is the enemy, and the Arab
        /// sides fight on one team (Movement's reading of "the enemy").</summary>
        static bool Player(Unit u) => u.Side == "" || u.Side == Movement.PlayerSide;

        /// <summary>Only military units fight (R-UNIT-001): a halutz is never a target or an attacker.</summary>
        static bool Military(Unit u) => u.Side != "";

        static string Hex(int q, int r) => string.Format(CultureInfo.InvariantCulture, "{0},{1}", q, r);

        static bool Adjacent(Unit u, int q, int r) => Movement.Neighbours.Any(d => u.Q + d.Q == q && u.R + d.R == r);

        static UnitType? TypeOf(Content c, Unit u) => c.UnitTypes.FirstOrDefault(t => t.Id == u.Type);

        /// <summary>R-COMBAT-007: a unit of the artillery class (Q-ARTY-1).</summary>
        static bool Guns(Content c, Unit u) => TypeOf(c, u)?.Class == ArtilleryClass;

        /// <summary>R-COMBAT-007: artillery reaches an enemy hex within its type's range; every other unit only the hex next
        /// to it (Q-COMBAT-2).</summary>
        static bool InReach(Content c, Unit u, int q, int r)
        {
            if (!Guns(c, u)) return Adjacent(u, q, r);
            var d = MapActions.Distance(u.Q, u.R, q, r);
            return d >= 1 && d <= TypeOf(c, u)!.Range!.Value;   // the loader requires a range on an artillery type
        }

        /// <summary>The Arms the player's unit spends when it joins an engagement: its type's arms_per_fire for artillery
        /// (R-COMBAT-007), else mvp.arms_per_attack (R-COMBAT-004).</summary>
        static int Need(Content c, MvpRules mvp, Unit u) => Guns(c, u) ? TypeOf(c, u)!.ArmsPerFire!.Value : mvp.ArmsPerAttack!.Value;

        /// <summary>R-COMBAT-007, R-COMBAT-008: the percent of its strength a unit fights at, its type's combat_percent or 100.</summary>
        static long Factor(Content c, Unit u) => TypeOf(c, u)?.CombatPercent ?? 100;

        /// <summary>R-COMBAT-009: the truce cards and the ends of their windows, half-open like R-ENEMY-006's (Q-WAR-3;
        /// owner, 22 October 2026, #750): from accepting first_truce to 8 July 1948, and from accepting second_truce to
        /// 15 October 1948, the sourced dates of first_truce.yaml, ten_days.yaml, second_truce.yaml and
        /// operation_yoav.yaml. The player's side is bound from accepting the card, not by date alone: a refused or
        /// defied card binds nothing.</summary>
        internal static readonly (string Event, string Option, DateTime Until)[] PlayerTruces =
        {
            ("first_truce", "accept", new DateTime(1948, 7, 8)),
            ("second_truce", "comply", new DateTime(1948, 10, 15)),
        };

        /// <summary>R-COMBAT-009: whether a truce the player accepted is in force on <see cref="GameState.Date"/> (the
        /// turn's first day, the date the HUD shows): until the turn that covers the window's end, so a complying
        /// player attacks again from the 15 October 1948 turn (owner ruling 1, 22 October 2026, #750).</summary>
        internal static bool TruceInForce(GameState s) =>
            PlayerTruces.Any(t => s.Date < t.Until && s.ChosenOptions.TryGetValue(t.Event, out var o) && o == t.Option);

        /// <summary>R-COMBAT-009: accepting a truce ends and clears every standing attack of the player's, with a
        /// line, the same as the enemy side's (owner ruling 2, 22 October 2026, #750; R-ENEMY-006).</summary>
        internal static void EndAttacksForTruce(GameState s, List<Cause> why)
        {
            foreach (var id in s.Units.Keys.ToList())
            {
                var u = s.Units[id];
                if (!Player(u) || u.Order != Attack) continue;
                s.Units[id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, "", "", u.HomeCity);
                why.Add(UnitLines.Line(WhyKeys.AttackEndedTruce,
                    $"Unit {id}'s attack on hex {u.OrderTarget} ends: a truce is on", new[] { u }, u.OrderTarget));
            }
        }

        /// <summary>The enemy military units on (q, r), seen from <paramref name="attacker"/>'s team, in id order.</summary>
        static List<Unit> Defenders(GameState s, Unit attacker, int q, int r) =>
            s.Units.Values.Where(x => x.Q == q && x.R == r && Military(x) && Player(x) != Player(attacker)).ToList();

        /// <summary>R-COMBAT-001's refusals: null when <see cref="Give"/> takes the attack on (q, r), else the reason shown to
        /// the player. Changes nothing: the unit card asks it for each enemy hex before it offers that hex (#754). Refused for
        /// an unknown unit, a halutz, an enemy unit, no hex, the attack it already has, a hex not next to it (artillery: beyond
        /// its range) or with no enemy unit, a truce the player accepted (R-COMBAT-009), or fewer Arms than one attack takes
        /// on top of the player's other attacks already ordered (Q-COMBAT-4). With <paramref name="enemy"/> the enemy AI orders its own unit through the same checks, the
        /// sides swapped, and pays no Arms (R-ENEMY-001; Q-ENEMY-4).</summary>
        public static string? Check(Content c, GameState s, int unitId, int? q, int? r, bool enemy = false)
        {
            if (!s.Units.TryGetValue(unitId, out var u)) return "There is no such unit";
            if (!Military(u)) return "Only a military unit takes that order";
            if (Player(u) == enemy) return "That unit is not yours";
            if (q == null || r == null) return "An attack names a hex";
            if (!enemy && TruceInForce(s)) return "A truce is in force";   // R-COMBAT-009
            var target = Hex(q.Value, r.Value);
            if (u.Order == Attack && u.OrderTarget == target) return "That unit already has that order";
            if (!InReach(c, u, q.Value, r.Value)) return Guns(c, u) ? "That hex is beyond the unit's range" : "That hex is not next to the unit";
            if (Defenders(s, u, q.Value, r.Value).Count == 0) return "There is no enemy unit on that hex";
            var mvp = c.RulesAt(s.Date).Mvp!;
            var need = Need(c, mvp, u);
            // Arms are paid at resolution, so the stock must also cover the player's other standing attacks, this turn's
            // included; the unit's own attack, which this order replaces, is not counted.
            var committed = s.Units.Values.Where(x => x.Id != unitId && x.Order == Attack && Military(x) && Player(x)).Sum(x => Need(c, mvp, x));
            return !enemy && s.Arms < need + committed ? Short(need, s.Arms, committed) : null;
        }

        /// <summary>R-COMBAT-001: gives the player's military unit a standing attack on the adjacent hex (q, r), replacing its
        /// order (a fortify is lost). Refused, state untouched, with <see cref="Check"/>'s reason. It costs no Attention (D-A6)
        /// and resolves in this turn's step 2. With <paramref name="enemy"/>, the enemy AI's own unit (see Check).</summary>
        internal static string? Give(Content c, GameState s, int unitId, int? q, int? r, List<Cause> why, bool enemy = false)
        {
            var refused = Check(c, s, unitId, q, r, enemy);
            if (refused != null) return refused;
            var u = s.Units[unitId]; var target = Hex(q!.Value, r!.Value);
            s.Units[unitId] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, Attack, target, u.HomeCity);
            why.Add(new Cause("map", unitId.ToString(CultureInfo.InvariantCulture), 0, $"Unit {unitId} will attack hex {target}"));
            return null;
        }

        static string Short(int need, int have, int committed) => committed == 0
            ? FormattableString.Invariant($"An attack needs {need} Arms and the stock has {have}")
            : FormattableString.Invariant($"An attack needs {need} Arms and the stock has {have}, of which {committed} go to attacks already ordered");

        /// <summary>The attack preview (spec section 2.6; Q-UI-1): the ratio, in percent and rounded down, of unit
        /// <paramref name="unitId"/>'s strength plus that of the units of its team already ordered to attack (q, r) from next to
        /// it (artillery: from within its range), each at its combat factor, against the enemy units there after fortify and
        /// terrain, and the combat_table row it falls in. Null when (q, r) is out of the unit's reach or holds no enemy unit, or
        /// the unit is unknown or no military unit. A gun previewed with no other attacker still gets a ratio, though step 2
        /// would have it hold its fire (R-COMBAT-007); the unit card shows it with <see cref="Outcomes"/> (#754).</summary>
        public static (int RatioPercent, int Row)? Preview(Content c, GameState s, int unitId, int q, int r)
        {
            if (!s.Units.TryGetValue(unitId, out var u) || !Military(u) || !InReach(c, u, q, r)) return null;
            var defenders = Defenders(s, u, q, r);
            if (defenders.Count == 0) return null;
            var target = Hex(q, r);
            var attackers = s.Units.Values.Where(x => x.Id == unitId
                || (x.Order == Attack && x.OrderTarget == target && Military(x) && Player(x) == Player(u) && InReach(c, x, q, r))).ToList();
            var mvp = c.RulesAt(s.Date).Mvp!;
            var ratio = Ratio(c, mvp, attackers, defenders, q, r);
            return (ratio, Row(mvp, ratio));
        }

        /// <summary>The outcome bands of combat_table row <paramref name="row"/> (Q-UI-1: the card shows them beside Preview's
        /// ratio): the row's faces with the same outcome taken together, in the order each first appears, each with how many
        /// faces of <c>Of</c> give it, the people the attackers and the defenders lose (population units times
        /// mvp.people_per_unit, as the engagement's report counts them) and whether the defenders retreat.</summary>
        public static IReadOnlyList<(int Faces, int Of, int AttackersLose, int DefendersLose, bool Retreat)> Outcomes(Content c, GameState s, int row)
        {
            var mvp = c.RulesAt(s.Date).Mvp!;
            var t = mvp.CombatTable[row];
            var bands = new List<(int Faces, int Of, int AttackersLose, int DefendersLose, bool Retreat)>();
            for (int i = 0; i < t.AttackerLoss.Count; i++)
            {
                var face = (t.AttackerLoss[i] * mvp.PeoplePerUnit, t.DefenderLoss[i] * mvp.PeoplePerUnit, t.DefenderRetreats[i] == 1);
                var k = bands.FindIndex(b => (b.AttackersLose, b.DefendersLose, b.Retreat) == face);
                if (k >= 0) bands[k] = (bands[k].Faces + 1, bands[k].Of, bands[k].AttackersLose, bands[k].DefendersLose, bands[k].Retreat);
                else bands.Add((1, t.AttackerLoss.Count, face.Item1, face.Item2, face.Item3));
            }
            return bands;
        }

        /// <summary>R-COMBAT-002: the attackers' summed strength over the defenders' summed strength, each unit's strength times
        /// its type's combat factor (R-COMBAT-007, R-COMBAT-008) and each defender's also times its order's defence percent
        /// (Orders.DefencePercent) and its hex's biome percent, in whole percent rounded down. Integer arithmetic only, so it
        /// is the same on every runtime.</summary>
        static int Ratio(Content c, MvpRules mvp, List<Unit> attackers, List<Unit> defenders, int q, int r)
        {
            long attack = attackers.Sum(x => x.Strength * Factor(c, x));   // in 1/100 of a strength unit
            var terrain = mvp.CombatTerrainPercent[c.HexAt(q, r)!.Biome];   // a unit stands on land, and the loader requires every land biome
            long defence = defenders.Sum(x => x.Strength * Factor(c, x) * Orders.DefencePercent(mvp, x) * terrain);   // in 1/1,000,000 of a strength unit
            return defence == 0 ? int.MaxValue : (int)Math.Min(int.MaxValue, attack * 1_000_000 / defence);   // 0: no real unit has no strength
        }

        /// <summary>The last combat_table row whose min_ratio_percent the ratio reaches (the first row starts at 0).</summary>
        static int Row(MvpRules mvp, int ratio)
        {
            var row = 0;
            for (int i = 1; i < mvp.CombatTable.Count; i++) if (ratio >= mvp.CombatTable[i].MinRatioPercent) row = i;
            return row;
        }

        /// <summary>Resolver step 2 (R-COMBAT-002 to R-COMBAT-006): in ascending attacker id, each unit with an attack order not
        /// yet in an engagement this turn leads one, with every unit of its team attacking the same hex. A unit no longer next
        /// to its hex (artillery: within its range), or whose hex holds no enemy unit, ends its attack; a player's unit pays
        /// its Arms when it joins, and one the stock cannot pay for has its attack called off (Q-COMBAT-4). Enemy units pay
        /// nothing (Q-ENEMY-4: no supply). Artillery joins after the other attackers and only beside one of them: alone it
        /// holds its fire, pays nothing and keeps its order (R-COMBAT-007). An engagement with an attacker left draws once.
        /// Afterwards every attack whose hex is now empty ends.</summary>
        public static void Resolve(Content c, GameState s, List<Cause> why)
        {
            var mvp = c.RulesAt(s.Date).Mvp!;
            var done = new HashSet<int>();
            foreach (var id in s.Units.Keys.ToList())   // Units is a SortedList: ascending id
            {
                if (done.Contains(id) || !s.Units.TryGetValue(id, out var lead) || lead.Order != Attack) continue;
                var (q, r) = Target(lead);
                // The guns last (a stable sort keeps id order on each side of it): they fire only beside another attacker.
                var group = s.Units.Values.Where(x => !done.Contains(x.Id) && x.Order == Attack && x.OrderTarget == lead.OrderTarget && Player(x) == Player(lead))
                    .OrderBy(x => Guns(c, x) ? 1 : 0).ToList();
                var defenders = Defenders(s, lead, q, r);
                var attackers = new List<Unit>();
                foreach (var x in group)
                {
                    done.Add(x.Id);
                    if (defenders.Count == 0 || !InReach(c, x, q, r)) { End(s, x, $"Unit {x.Id}'s attack on hex {x.OrderTarget} ends: no enemy unit is there", why); continue; }
                    if (Guns(c, x) && attackers.Count == 0)
                    {
                        why.Add(new Cause("map", x.Id.ToString(CultureInfo.InvariantCulture), 0, $"Unit {x.Id} holds its fire: no attack on hex {x.OrderTarget} to support"));
                        continue;
                    }
                    if (Player(x))
                    {
                        var need = Need(c, mvp, x);
                        if (s.Arms < need)
                        {
                            End(s, x, $"Unit {x.Id}'s attack on hex {x.OrderTarget} is called off: it needs {need.ToString(CultureInfo.InvariantCulture)} Arms and the stock has {s.Arms.ToString(CultureInfo.InvariantCulture)}", why);
                            continue;
                        }
                        s.Arms -= need;
                    }
                    attackers.Add(x);
                }
                if (attackers.Count > 0) Engage(c, s, mvp, attackers, defenders, q, r, why);
            }
            foreach (var u in s.Units.Values.ToList())
                if (u.Order == Attack)
                {
                    var (q, r) = Target(u);
                    if (Defenders(s, u, q, r).Count == 0 || !InReach(c, u, q, r))
                        End(s, u, $"Unit {u.Id}'s attack on hex {u.OrderTarget} ends: no enemy unit is there", why);
                }
        }

        static (int Q, int R) Target(Unit u)
        {
            var parts = u.OrderTarget.Split(',');
            return (int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
        }

        static void End(GameState s, Unit u, string line, List<Cause> why)
        {
            s.Units[u.Id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, "", "", u.HomeCity);
            why.Add(new Cause("map", u.Id.ToString(CultureInfo.InvariantCulture), 0, line));
        }

        /// <summary>R-COMBAT-002, R-COMBAT-003, R-COMBAT-006: one engagement. The face is the one draw; its row's losses fall on
        /// each side one population unit at a time on its strongest unit (a tie on the lower id), a unit at 0 leaves the state,
        /// and when the row says so each defender left retreats one hex, in id order, or surrenders when it has nowhere to go.
        /// The attackers stay where they are (no advance, Q-COMBAT-3).</summary>
        static void Engage(Content c, GameState s, MvpRules mvp, List<Unit> attackers, List<Unit> defenders, int q, int r, List<Cause> why)
        {
            var ratio = Ratio(c, mvp, attackers, defenders, q, r);
            var row = mvp.CombatTable[Row(mvp, ratio)];
            var rng = new Rng(s.RngState);
            var face = (int)(rng.NextDouble() * row.AttackerLoss.Count);   // DrawsPerEngagement
            s.RngState = rng.State;
            var target = Hex(q, r);
            var ppu = mvp.PeoplePerUnit;
            var removed = new List<int>();
            // R-COMBAT-007: the guns fire from behind the line, so the attackers' losses fall on the others only.
            var attackersLost = Take(s, attackers.Where(x => !Guns(c, x)).Select(x => x.Id).ToList(), row.AttackerLoss[face], removed);
            var defendersLost = Take(s, defenders.Select(x => x.Id).ToList(), row.DefenderLoss[face], removed);
            why.Add(new Cause("map", target, 0, FormattableString.Invariant(
                $"The attack on hex {target} ({Ids(attackers)} against {Ids(defenders)}, strength ratio {ratio}%): the attackers lost {attackersLost * ppu:N0} people and the defenders lost {defendersLost * ppu:N0} people"),
                // #1297: the turn report names an enemy attack on every turn it engages, a standing one too (R-ENEMY-003)
                Player(attackers[0]) ? "" : Attack, Player(attackers[0]) ? null : attackers.Select(x => x.Id).OrderBy(x => x).ToList()));
            foreach (var id in removed)
                why.Add(new Cause("map", id.ToString(CultureInfo.InvariantCulture), 0, $"Unit {id} was destroyed"));
            if (row.DefenderRetreats[face] != 1) return;
            foreach (var d in defenders)
            {
                if (!s.Units.TryGetValue(d.Id, out var u)) continue;
                if (Retreat(c, s, mvp, u) is (int, int) to)
                {
                    s.Units[u.Id] = new Unit(u.Id, u.Type, to.Item1, to.Item2, u.Service, u.Side, u.Strength, u.Order, u.OrderTarget, u.HomeCity);
                    why.Add(new Cause("map", u.Id.ToString(CultureInfo.InvariantCulture), 0, $"Unit {u.Id} retreated to hex {Hex(to.Item1, to.Item2)}"));
                }
                else
                {
                    // The only way a unit is captured (impact plan section 4.3 item 1; R-POW-001). Its captor is the engagement's
                    // lowest-id attacker (not attackers[0]: the guns are listed last, R-COMBAT-007), whose side holds the
                    // player's soldiers (R-POW-005).
                    s.Units.Remove(u.Id);
                    var held = Prisoners.Take(s, u, attackers.OrderBy(x => x.Id).First());
                    why.Add(new Cause("map", u.Id.ToString(CultureInfo.InvariantCulture), 0,
                        FormattableString.Invariant($"Unit {u.Id} surrendered with {u.Strength * ppu:N0} people: it had no hex to retreat to. {held}")));
                }
            }
        }

        // Sorted for the text: step 2 puts the guns last (R-COMBAT-007), but the line names the units in id order.
        static string Ids(List<Unit> units) => (units.Count == 1 ? "unit " : "units ") + string.Join(", ", units.Select(x => x.Id).OrderBy(x => x).Select(x => x.ToString(CultureInfo.InvariantCulture)));

        /// <summary>Takes up to <paramref name="loss"/> population units from <paramref name="ids"/>, one at a time from the
        /// strongest unit left (a tie on the lower id; the list is in id order), removing each that reaches 0 (its id is never
        /// reused: NextId is not touched) and adding it to <paramref name="removed"/>. Returns the units taken.</summary>
        static int Take(GameState s, List<int> ids, int loss, List<int> removed)
        {
            int taken = 0;
            for (; taken < loss; taken++)
            {
                Unit? top = null;
                foreach (var id in ids)
                    if (s.Units.TryGetValue(id, out var x) && (top == null || x.Strength > top.Strength)) top = x;
                if (top == null) break;
                if (top.Strength <= 1) { s.Units.Remove(top.Id); removed.Add(top.Id); }
                else s.Units[top.Id] = new Unit(top.Id, top.Type, top.Q, top.R, top.Service, top.Side, top.Strength - 1, top.Order, top.OrderTarget, top.HomeCity);
            }
            return taken;
        }

        /// <summary>R-COMBAT-003: the first neighbour of the unit's hex, in the fixed order (R-MOVE-002), that is land, holds no
        /// unit of the other team, has room under mvp.stack_limit for the unit's side (R-MOVE-003) and, for the player's unit,
        /// is not an enemy-held locality (R-MOVE-005); null when there is none.</summary>
        static (int, int)? Retreat(Content c, GameState s, MvpRules mvp, Unit u)
        {
            var noEntry = Player(u) ? Movement.NoEntry(c, s) : new HashSet<(int Q, int R)>();
            foreach (var (dq, dr) in Movement.Neighbours)
            {
                int q = u.Q + dq, r = u.R + dr;
                if (!(c.HexAt(q, r) is MapHex hex) || !hex.Land || noEntry.Contains((q, r))) continue;
                var here = s.Units.Values.Where(x => x.Q == q && x.R == r).ToList();
                if (here.Any(x => Player(x) != Player(u)) || here.Count(x => x.Side == u.Side) >= mvp.StackLimit!.Value) continue;
                return (q, r);
            }
            return null;
        }
    }
}
