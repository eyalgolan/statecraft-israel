using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>One R-ENEMY-007 operation-start row: the starting-force row and the allowed player regions, in ordinal order.</summary>
    public sealed class OperationRequirementRow
    {
        public string Unit { get; }
        public IReadOnlyList<string> Regions { get; }
        internal OperationRequirementRow(string unit, IReadOnlyList<string> regions) { Unit = unit; Regions = regions; }
    }

    /// <summary>A pure snapshot of an operation option's authored requirements in the current state.</summary>
    public sealed class OperationRequirementStatus
    {
        public IReadOnlyList<OperationRequirementRow> Required { get; }
        public IReadOnlyList<OperationRequirementRow> Missing { get; }
        public bool Met => Missing.Count == 0;
        internal OperationRequirementStatus(IReadOnlyList<OperationRequirementRow> required, IReadOnlyList<OperationRequirementRow> missing)
        { Required = required; Missing = missing; }
    }

    public static class EventSystem
    {
        public const int MaxSoftEventsPerTurn = 3;
        public const string RefusedOperationUnitsNotInPlace = "The operation's required units are not in place";
        /// <summary>Q3 (owner, 7 November 2026: "Decide armistice first (Recommended)"): an operation whose front's armistice card
        /// is pending waits for it; once that armistice is signed, the operation cannot be chosen (it lapses at the turn's end).</summary>
        public const string RefusedOperationArmisticeFirst = "The armistice on this operation's front must be decided first";
        public const string RefusedOperationArmisticeSigned = "The armistice on this operation's front is signed";
        /// <summary>The facility a grant with no `at` stands at (Q-HFX-2) and the city fields an option may change (R-EFX-004).</summary>
        public const string MilitaryCamp = "military_camp", HousingField = "housing_capacity", JobsField = "job_slots", ConditionField = "condition";

        static GameEvent Get(Content c, string id) => c.Events.First(e => e.Id == id);

        /// <summary>Fires every unfired event whose `requires` (if any) is met, whose window [From, To) overlaps the turn just played,
        /// [<paramref name="turnStart"/>, s.Date), or contains s.Date; <paramref name="turnStart"/> defaults to s.Date
        /// (game start). A quiet armistice card (R-WAR-008, gdd-0.3) is the exception: it can fire before its From and after its To.
        /// Every eligible hard anchor fires; MaxSoftEventsPerTurn caps only the non-hard events per turn,
        /// which fill the slots that remain (max(0, cap - hard count)) in the usual order (an early armistice first, R-WAR-008, then consequence cards, R-ALT-008, then From, objective cards last in objectives.yaml's order, R-OBJ-007, then id). Objective and Deport cards take no soft slot. The fired
        /// events keep the usual order. An eligible soft event left out by the cap waits past its window until it can fire.
        /// Late fire: an event whose window overlapped the turn just played can fire after its window has ended; its
        /// conditions are evaluated at the end of the turn, and its text is filled at the last in-window day
        /// (<see cref="FillDate"/>).
        /// <para>With <paramref name="endingTurn"/> (the turn that ended the game, R-LOOP-004) only the unfired cards of recorded
        /// objectives fire (those that turn recorded, and any recorded earlier whose card has not fired yet), all of them, uncapped
        /// (R-OBJ-003; the owner's choice "Show the card (Recommended)", 4 November 2026, #2536); no other event does.</para></summary>
        public static void Trigger(Content c, GameState s, List<Cause> why, DateTime? turnStart = null, bool endingTurn = false)
        {
            PopulationLedger.RefuseAggregate(s);
            var start = turnStart ?? s.Date;
            // R-WAR-008 (#2478): under gdd-0.3 an armistice card also comes while its own front may have an early offer
            // (Armistice.MayOfferEarly): before its hard-anchor date, and again after a refusal. On that path it is soft.
            var quiet = new HashSet<string>(c.Events.Where(e => e.FrontQuiet != "" && Armistice.MayOfferEarly(c, s, e.FrontQuiet)
                && (s.FiredEvents.Contains(e.Id) || s.Date < e.From)).Select(e => e.Id));
            // R-WAR-008 (#2813): a quiet armistice card has no end date, so its `to` (1950-01-01 on the real cards) does not end it.
            var eligible = c.Events
                .Where(e => (quiet.Contains(e.Id) || !s.FiredEvents.Contains(e.Id) && e.From <= s.Date)
                            && (quiet.Contains(e.Id) || s.CappedEvents.Contains(e.Id) || e.To == null || start < e.To.Value || c.IsGdd && e.Follows))
                .Where(e => !endingTurn || EndingCard(s, e))
                // R-ENEMY-007 (owner, 7 November 2026: "They drop (Recommended)", "Signing wins (Recommended)"): a card whose front's
                // armistice is signed never first fires after it. Only the GDD loop has such cards.
                .Where(e => e.ArmisticeFront == "" || !Demobilization.Signed(s, e.ArmisticeFront))
                // R-ENEMY-007: unlike an ordinary late-fired card, an operation cannot first appear after it is no longer relevant.
                .Where(e => !c.IsGdd || e.To == null || s.Date < e.To.Value || !e.Options.Any(o => o.RequiresUnitRegions.Count != 0))
                .Where(e => Timeline.AllMet(c, s, e))
                // R-OBJ-003: an objective's card waits for the objective. Only the GDD loop records one, so in the legacy loop it never fires.
                .Where(e => e.Objective == "" || s.Objectives.ContainsKey(e.Objective))
                // R-SWAMP-002: an event about a site's works waits for those works, and stops once the site is built (drained).
                // The legacy loop reads no sites, so it ignores the key and the legacy goldens hold.
                .Where(e => e.Site == "" || !c.IsGdd || (s.Sites.TryGetValue(e.Site, out var works) && works.BuiltOn == null))
                // R-POW-004: a front's exchange card waits for prisoners held either way. Only the GDD loop takes any, so the
                // legacy loop never fires one.
                .Where(e => e.Prisoners == "" || Prisoners.Held(s, e.Prisoners))
                // R-EVT-007: a Deport follow-up card waits for the player's Deport of its place. Only the GDD loop records
                // settlements, so the legacy loop never fires one.
                .Where(e => e.Deported == "" || (s.Settlements.TryGetValue(e.Deported, out var place) && place.Depopulated))
                // A late fire can come after a dated indicator's window has ended (chapters design 3.2): a condition on it is not met.
                .Where(e => e.Conditions.All(k => s.Indicators.TryGetValue(k.Indicator, out var v)
                                               && (!k.Min.HasValue || v >= k.Min.Value) && (!k.Max.HasValue || v <= k.Max.Value)))
                // R-ALT-008: a road the player took is not crowded out of its window. With no consequence card eligible the first
                // key is the same for every event, and (From, id) is a total order (ids are unique), so the order is as before.
                // R-OBJ-007 (Q-CARD-2): objective cards of the same From come after that From's other events, in objectives.yaml's order.
                // R-EVT-006: then an event's `order` (default 0), then the id, so with every order 0 the order is as before.
                // R-EVT-002 (Q-EVT-1, Q-OBJ-1): an early armistice comes before even the consequence cards; with none, as before.
                .OrderBy(e => quiet.Contains(e.Id) ? 0 : e.Kind == EventKinds.Consequence ? 1 : 2).ThenBy(e => e.From)
                .ThenBy(e => e.Objective == "" ? -1 : c.Objectives.FindIndex(o => o.Id == e.Objective)).ThenBy(e => e.Order)
                .ThenBy(e => e.Id, StringComparer.Ordinal)
                .ToList();
            // R-OBJ-003, R-EVT-007: objective and Deport cards take no soft slot, so they fire however full the turn is.
            // A hard objective or Deport card takes no soft slot either, so it does not shrink the slots the others share.
            // R-WAR-008: an early armistice is soft (it takes a slot and can wait); on its date it is a hard anchor.
            bool Hard(GameEvent e) => e.Anchor == "hard" && !quiet.Contains(e.Id) && e.Objective == "" && e.Deported == "";
            bool TakesSoftSlot(GameEvent e) => !Hard(e) && e.Objective == "" && e.Deported == "";
            var hardCount = eligible.Count(Hard);
            var chosen = new HashSet<string>(eligible.Where(e => !TakesSoftSlot(e))
                .Concat(eligible.Where(TakesSoftSlot).Take(endingTurn ? int.MaxValue : Math.Max(0, MaxSoftEventsPerTurn - hardCount))).Select(e => e.Id));
            // R-EVT-001: only events eligible now can newly enter the wait; every other gate is re-evaluated on later turns.
            foreach (var e in eligible.Where(e => TakesSoftSlot(e) && !chosen.Contains(e.Id))) s.CappedEvents.Add(e.Id);
            foreach (var e in eligible.Where(e => chosen.Contains(e.Id)))
            {
                s.CappedEvents.Remove(e.Id);
                s.FiredEvents.Add(e.Id);
                s.PendingEvents.Add(e.Id);
                why.Add(new Cause("events", e.Id, 0, $"New: {Title(c, s, e)}", key: CauseKeys.EventNew, args: Context(Title(c, s, e), HebrewTitle(c, s, e))));
            }
        }

        /// <summary>Whether <paramref name="e"/> is a card of the turn that ended the game: the card of a recorded objective, in a
        /// game that is over (R-OBJ-003, R-LOOP-004; #2536). Not only the objectives that turn recorded: the card of one recorded
        /// earlier that has not fired yet has no later turn to wait for. Unfired ones are the only events that
        /// fire on that turn; Unity shows them before the end screen.</summary>
        public static bool EndingCard(GameState s, GameEvent e) =>
            s.GameOver != null && e.Objective != "" && s.Objectives.ContainsKey(e.Objective);

        /// <summary>The date whose office holders an event's text names: the given date, or the last in-window day
        /// once the window has ended (load-time coverage guarantees a holder there).</summary>
        public static DateTime FillDate(GameEvent e, DateTime date) => e.To.HasValue && date >= e.To.Value ? e.To.Value.AddDays(-1) : date;

        /// <summary>R-ENEMY-005 (the owner's answer 1 on #2087, "Orders expire with the operation"): the day a card's order to a
        /// row's anchor hex ends, the `to` of that anchor's window in map/units_1948.yaml (the latest, should two anchors share
        /// the hex), or null when it has none. The anchor's dates, not the card's: a card's from/to is when the decision is open
        /// (Horev's closes on 23 December 1948; the operation ran to 7 January 1949), while the anchor's are the sourced span of
        /// the position the operation sends the unit to: Carmeli facing the bridgehead to 19 July 1948, the end of Brosh, and
        /// Golani at the Julis camp to 24 February 1949, the Egypt armistice. An order target's `to` counts as an anchor's
        /// (owner, 2 November 2026, #2087, option 1).</summary>
        internal static DateTime? OrderEnds(Content c, UnitOrderEffect x) =>
            c.UnitAnchorHexes.Where(k => k.Key.Row == x.Unit && k.Value == (x.Q, x.R) && c.UnitAnchorEnds.ContainsKey(k.Key))
                .Select(k => (DateTime?)c.UnitAnchorEnds[k.Key])
                .Concat(c.UnitOrderTargets.Where(k => k.Key.Row == x.Unit && (k.Value.Q, k.Value.R) == (x.Q, x.R)).Select(k => (DateTime?)k.Value.To))
                .Max();

        /// <summary>R-ENEMY-007: every distinct unit_orders row must have its starting unit in any one of the option's
        /// authored regions. Missing, destroyed or replaced starting units are not in place. Pure: no state or RNG change.</summary>
        public static OperationRequirementStatus OperationStatus(Content c, GameState s, EventOption option)
        {
            if (!c.IsGdd)
                return new OperationRequirementStatus(Array.Empty<OperationRequirementRow>(), Array.Empty<OperationRequirementRow>());
            var regions = option.RequiresUnitRegions.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var required = option.UnitOrders.Select(x => x.Unit).Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal).Select(x => new OperationRequirementRow(x, regions)).ToArray();
            var missing = required.Where(row =>
            {
                int id = c.StartingForces.FindIndex(f => f.Row == row.Unit);
                if (id < 0 || !s.Units.TryGetValue(id, out var unit)) return true;
                var force = c.StartingForces[id];
                if (unit.Side != force.Side || unit.Type != force.Type) return true;
                var region = c.PlayerRegions.AtHex(unit.Q, unit.R);
                return region == null || !row.Regions.Contains(region.Id, StringComparer.Ordinal);
            }).ToArray();
            return new OperationRequirementStatus(required, missing);
        }

        /// <summary>R-ENEMY-009: the decided operation card whose go-to order unit <paramref name="unitId"/> is carrying out, or
        /// null. The operation binds the unit while the card's go-to stands (Movement.GiveGoTo's card order): the card is the one
        /// whose decided option sends the unit's map/units_1948.yaml row to the hex the unit is going to, which the loader keeps
        /// unique (EventRules.CheckOperationHexes). Pure: no state or RNG change.</summary>
        public static string? OperationOf(Content c, GameState s, int unitId)
        {
            if (!c.IsGdd || !s.CardOrders.ContainsKey(unitId) || !s.Units.TryGetValue(unitId, out var u) || u.Order != Movement.GoTo
                || unitId < 0 || unitId >= c.StartingForces.Count)   // the unit with id i is the i-th force (R-UNIT-006)
                return null;
            var force = c.StartingForces[unitId];
            if (u.Side != force.Side || u.Type != force.Type) return null;
            foreach (var e in c.Events)
                if (s.ChosenOptions.TryGetValue(e.Id, out var chosen) && e.Options.FirstOrDefault(o => o.Id == chosen) is EventOption option
                    && option.UnitOrders.Any(x => x.Order == Movement.GoTo && x.Unit == force.Row && Hex(x.Q, x.R) == u.OrderTarget))
                    return e.Id;
            return null;
        }

        /// <summary>R-ENEMY-009: the units, in ascending id, still carrying out operation card <paramref name="eventId"/>'s
        /// orders (<see cref="OperationOf"/>): the units a player's order to any one of them would stop. Pure.</summary>
        public static IReadOnlyList<int> OperationUnits(Content c, GameState s, string eventId) =>
            s.CardOrders.Keys.Where(id => OperationOf(c, s, id) == eventId).ToList();

        /// <summary>R-ENEMY-009 (owner, 4 November 2026; on #2544, "Only remaining orders (Recommended)"): a player's order took a
        /// unit of operation <paramref name="eventId"/> off its order, so the operation's remaining orders are cancelled. Each
        /// unit of <paramref name="bound"/> (the operation's units before that order) still carrying the card's go-to stops where
        /// it stands, with the line of an ended card order; the moves made and the fights fought stay. Every one of them,
        /// the ordered unit too, is listed as withdrawn. No RNG.</summary>
        internal static void CancelOperation(Content c, GameState s, string eventId, IReadOnlyList<int> bound, List<Cause> why, List<OperationAction>? actions)
        {
            var option = Get(c, eventId).Options.First(o => o.Id == s.ChosenOptions[eventId]);
            foreach (var id in bound)
            {
                var row = c.StartingForces[id].Row;
                var i = option.UnitOrders.FindIndex(x => x.Order == Movement.GoTo && x.Unit == row);
                var x = option.UnitOrders[i];
                if (OperationOf(c, s, id) == eventId) Movement.Withdraw(s, id, why);
                actions?.Add(new OperationAction(eventId, option.Id, i, row, id, Movement.GoTo, x.Q, x.R, OperationAction.Withdrawn));
            }
        }

        static string Hex(int q, int r) => FormattableString.Invariant($"{q},{r}");

        /// <summary>Returns null on success, or the reason the choice was rejected. <paramref name="spent"/> is the movement
        /// points the turn's orders have spent so far, which an option's unit orders share (R-ENEMY-005).
        /// A refused unit order goes into <paramref name="rejected"/>, as the player's refused orders do, and each of the option's
        /// unit orders into <paramref name="actions"/> (R-ENEMY-008). An attack is recorded in <paramref name="attacked"/>, as
        /// the player's is.</summary>
        public static string? Choose(Content c, GameState s, string eventId, string optionId, List<Cause> why, Dictionary<int, int>? spent = null, List<Rejection>? rejected = null,
                                     List<OperationAction>? actions = null, Dictionary<int, AttackRecord>? attacked = null)
        {
            PopulationLedger.RefuseAggregate(s);
            if (!s.PendingEvents.Contains(eventId)) return "That event is not waiting for a decision";
            var option = Get(c, eventId).Options.FirstOrDefault(o => o.Id == optionId);
            if (option == null) return "That event has no such option";
            if (ArmisticeBlock(c, s, Get(c, eventId), option) is string block) return block;
            if (!OperationStatus(c, s, option).Met) return RefusedOperationUnitsNotInPlace;
            Resolve(c, s, eventId, option, "chosen", why, spent: spent, rejected: rejected, actions: actions, attacked: attacked);
            return null;
        }

        /// <param name="share">The share of each numeric effect that applies: 1, except for a disloyal minister's delegation (R-EVT-003).</param>
        static void Resolve(Content c, GameState s, string eventId, EventOption o, string how, List<Cause> why, double share = 1.0, bool delegated = false, Person? minister = null, Dictionary<int, int>? spent = null, List<Rejection>? rejected = null,
                            List<OperationAction>? actions = null, Dictionary<int, AttackRecord>? attacked = null)
        {
            s.PendingEvents.Remove(eventId);
            // R-ENEMY-007: a decided card waits no more. Q4 (owner, 7 November 2026: "Shift by the delay (Recommended)"): a card
            // that waited launches its orders to end later by the time it waited, from its first postponement to now.
            var delay = s.PostponedOperations.TryGetValue(eventId, out var waited) ? s.Date - waited.Date : TimeSpan.Zero;
            s.PostponedOperations.Remove(eventId);
            s.ChosenOptions[eventId] = o.Id;
            var fillDate = FillDate(Get(c, eventId), s.Date);
            var title = Title(c, s, Get(c, eventId));
            var variant = Timeline.Variant(c, s, Get(c, eventId));
            var titleHe = variant == null || variant.Title == "";   // the event's title_he names it, as on its card (B1)
            var localizedTitle = HebrewTitle(c, s, Get(c, eventId));
            why.Add(new Cause("events", eventId, 0, $"{how}: {Cabinet.Fill(c, s, o.Text, fillDate)}",
                key: !delegated ? CauseKeys.EventChosen : share < 1 ? CauseKeys.EventDelegatedPartial : CauseKeys.EventDelegated,
                args: new[] { Cabinet.Fill(c, s, o.Text, fillDate), SnapshotText(c, s, fillDate, o.Text, o.TextHe),
                    minister?.Name ?? "", minister == null ? "" : LocalName(minister) }));
            foreach (var fx in o.Effects.Where(fx => Reaches(s, fx.Target)))
            {
                var delta = fx.Delta * share;
                var change = delta;
                if (fx.Target == "treasury")
                {
                    // R-MONEY-003: the cost first, then what was borrowed for it, as every other payment borrows.
                    why.Add(new Cause("events", fx.Target, change, $"{PlayerText.Change(c, fx.Target, change)} ({title})",
                        key: CauseKeys.EventAmount, args: Context(title, localizedTitle)));
                    if (delta < 0) EconomySystem.PayNamedOrBorrow(c, s, -delta, $"for {title}", why, CauseKeys.Purpose.Event, eventId, title, titleHe ? "1" : "0");
                    else s.Treasury += delta;
                    continue;
                }
                if (fx.Target == Pools.FoodTarget)
                {
                    // R-EFX-001: an event moves the national Food stock by whole units (the delta rounds half away
                    // from zero, and the loader bounds it to the int range). The stock stays in 0..int.MaxValue,
                    // computed in long so nothing wraps, and the cause carries the change that applied.
                    var want = (long)Math.Round(delta, MidpointRounding.AwayFromZero);
                    var applied = Math.Max(-(long)s.Food, Math.Min(want, (long)int.MaxValue - s.Food));
                    s.Food = (int)(s.Food + applied);
                    change = applied;
                }
                else if (fx.Target == Pools.ArmsTarget)
                {
                    // R-EFX-001 (#781): the Arms stock moves exactly as the Food stock does above.
                    var want = (long)Math.Round(delta, MidpointRounding.AwayFromZero);
                    var applied = Math.Max(-(long)s.Arms, Math.Min(want, (long)int.MaxValue - s.Arms));
                    s.Arms = (int)(s.Arms + applied);
                    change = applied;
                }
                else if (fx.Target == "fx_reserves") s.FxReserves += delta;
                else change = EffectsSystem.Shift(c, s, fx.Target, delta);
                why.Add(new Cause("events", fx.Target, change, $"{PlayerText.Change(c, fx.Target, change)} ({title})",
                    key: fx.Target == Pools.FoodTarget || fx.Target == Pools.ArmsTarget ? CauseKeys.EventStock : CauseKeys.EventAmount, args: Context(title, localizedTitle)));
            }
            // R-ENEMY-007 (owner, 7 November 2026, on a late Ten Days during the truce: "Same as Latrun, -0.10 (Recommended)"): an
            // operation that waited and launches while a truce the player accepted is in force breaks it, and costs International
            // Support as Latrun's offensive does; an option whose own effects already cost it (that offensive) is not charged again.
            if (c.IsGdd && delay > TimeSpan.Zero && o.RequiresUnitRegions.Count != 0 && Combat.TruceInForce(s) && Reaches(s, "intl_standing")
                && !o.Effects.Any(fx => fx.Target == "intl_standing" && fx.Delta < 0))
            {
                var broke = EffectsSystem.Shift(c, s, "intl_standing", c.RulesAt(s.Date).Mvp!.OperationTruceBreakStanding!.Value);
                why.Add(new Cause("events", "intl_standing", broke, $"{PlayerText.Change(c, "intl_standing", broke)} ({title})",
                    key: CauseKeys.EventAmount, args: Context(title, localizedTitle)));
            }
            // R-EFX-009 (#2457): the bond's dollars also go onto Debt, at the event's date's rate, scaled like the numeric effects.
            if (DebtFor(c, Get(c, eventId), o, s.Date) is double debt)
            {
                s.Debt += debt * share;
                why.Add(new Cause("events", "debt", debt * share, $"{PlayerText.Change(c, "debt", debt * share)} ({title})",
                    key: CauseKeys.EventAmount, args: Context(title, localizedTitle)));
            }
            // R-EFX-006: the structured effects after the numeric ones, in list order. The loader has checked every target.
            foreach (var g in o.GrantUnits)
            {
                // R-EFX-003 (Q-HFX-2): free; the deal's cost is the option's numeric effects. The units take the next ids.
                var (place, q, r) = GrantPlace(c, g);
                for (int i = 0; i < g.N; i++) { var id = s.NextId++; s.Units.Add(id, new Unit(id, g.Type, q, r)); }
                why.Add(new Cause("events", g.Type, g.N, $"{Granted(g, place)} ({title})",
                    key: CauseKeys.EventGrant, args: Context(title, localizedTitle, GrantCity(c, g))));
            }
            foreach (var x in o.CityEffects)
            {
                // R-EFX-004: only the named field changes. Housing capacity stays at 0 or more; job slots never fall below the
                // city's employed, so a cut takes only free slots; the condition stays from 0 to 1 (R-HOUSE-001). The loader
                // has checked the capacity and slot deltas are whole. The cause carries the change that applied.
                var city = s.Cities[x.City];
                if (x.Field == ConditionField)
                {
                    // A hand-built test city may have no condition; every GDD game's cities have one (R-HOUSE-001).
                    if (city.Condition is not double now) continue;
                    var condition = Math.Min(1.0, Math.Max(0.0, now + x.Delta));
                    s.Cities[x.City] = city.WithCondition(condition);
                    why.Add(new Cause("events", x.City + "." + x.Field, condition - now, $"{CityChange(c, x.City, x.Field, condition - now)} ({title})",
                        key: CauseKeys.EventCity, args: Context(title, localizedTitle, x.City, x.Field)));
                    continue;
                }
                var delta = (int)x.Delta;
                var housing = x.Field == HousingField
                    ? (int)Math.Max(0L, Math.Min((long)int.MaxValue, (long)city.HousingCapacity + delta))
                    : city.HousingCapacity;
                var jobs = x.Field == JobsField
                    ? (int)Math.Max((long)city.Employed, Math.Min((long)int.MaxValue, (long)city.JobSlots + delta))
                    : city.JobSlots;
                s.Cities[x.City] = new City(city.Id, housing, jobs, city.Civilians, city.Employed, city.Refugees, city.Recruits, city.Condition, city.Craftsmen, city.JobsDefault, city.Qualified);
                var applied = (long)housing - city.HousingCapacity + (long)jobs - city.JobSlots;
                why.Add(new Cause("events", x.City + "." + x.Field, applied, $"{CityChange(c, x.City, x.Field, applied)} ({title})",
                    key: CauseKeys.EventCity, args: Context(title, localizedTitle, x.City, x.Field)));
            }
            // R-EFX-008: after the city effects. A kind already unlocked stays so, with no second cause.
            foreach (var kind in o.UnlockProjects)
                if (s.UnlockedProjects.Add(kind)) why.Add(new Cause("events", kind, 1, $"{Unlocked(c, kind)} ({title})", key: CauseKeys.EventUnlock, args: Context(title, localizedTitle)));
            // R-ENEMY-005 (Q-ENEMY-5), R-ENEMY-008: the option's go-to and attack orders, through the player's own go-to and
            // attack (Combat.Strike: the turn's movement points, one engagement, one draw) and their checks, in list order (the
            // owner, 4 November 2026: "deciding on a military operation makes the units do what the operation describes").
            // A refused order changes nothing, as the player's does, and is reported as the player's are (Rejected), with the
            // card's reason; the orders after it still run (on #2544: "Continue the rest (Recommended)"). Each order is listed
            // in `actions` with the player's own reason. A row with no unit on the board orders nobody. R-ENEMY-007 applies the
            // orders under both GDD identities once every required unit is in place. The owner's answer 1 on #2087: a go-to
            // ends with the operation (OrderEnds), and
            // its march goes round every enemy-held place but the one it was sent to (Movement.Search). The owner's ruling of
            // 2 November 2026 on #2087 ("Keep a garrison in Nazareth (Recommended)"): the order never takes the last Israeli unit
            // off an occupied place (R-CTRL-013), so it is not given to that unit, and the refusal says why.
            var points = spent ?? new Dictionary<int, int>();
            for (int i = 0; i < o.UnitOrders.Count; i++)
            {
                var x = o.UnitOrders[i];
                int id = c.StartingForces.FindIndex(f => f.Row == x.Unit);   // the unit with id i is the i-th force (R-UNIT-006)
                if (id < 0 || !s.Units.TryGetValue(id, out var u) || u.Side != c.StartingForces[id].Side || u.Type != c.StartingForces[id].Type) continue;
                // An attacker never leaves its hex (R-COMBAT-001), so the last-garrison rule is a go-to's alone.
                var reason = x.Order == Combat.Attack ? Combat.Strike(c, s, id, x.Q, x.R, points, attacked ?? new Dictionary<int, AttackRecord>(), why)
                    : SettlementControl.LastGarrison(c, s, u) ? Orders.RefusedLastGarrison
                    : Movement.GiveGoTo(c, s, id, x.Q, x.R, points, why, OrderEnds(c, x) + delay);
                if (reason != null)
                    rejected?.Add(new Rejection(new UnitOrder(id, x.Order, x.Q, x.R), reason == Orders.RefusedLastGarrison ? reason : Orders.RefusedCardOrder));
                actions?.Add(new OperationAction(eventId, o.Id, i, x.Unit, id, x.Order, x.Q, x.R,
                    reason == null ? OperationAction.CarriedOut : OperationAction.Refusal, reason));
            }
            foreach (var kv in o.SetPolicy.Where(k => s.Policies.ContainsKey(k.Key)).OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                s.Policies[kv.Key] = kv.Value;
                why.Add(new Cause("events", kv.Key, kv.Value, $"{PlayerText.Target(c, kv.Key)} set to {PlayerText.Percent(kv.Value)} ({title})",
                    key: CauseKeys.EventPolicy, args: Context(title, localizedTitle)));
            }
            if (o.SetRung.HasValue) SecuritySystem.SetRung(c, s, o.SetRung.Value, why);
            // R-POW-004: an exchange card returns its front's prisoners, whichever way it was resolved (it has one option).
            if (Get(c, eventId).Prisoners is var front && front != "") Prisoners.Return(c, s, front, why);
            Armistice.Resolved(c, s, eventId, o.Id);   // R-WAR-006, R-WAR-007: gdd-0.3 only
        }

        /// <summary>R-EFX-009 (owner, 2 November 2026, #2224: "Add equal Debt", "Rate in force on the date (Recommended)"): the
        /// IL millions of Debt the option's debt_dollars adds under gdd-0.3, at the mvp.il_per_dollar rate in force on the
        /// event's date (<see cref="FillDate"/>); null under any other ruleset or with no debt_dollars.</summary>
        static double? DebtFor(Content c, GameEvent e, EventOption o, DateTime date)
        {
            if (c.Ruleset != SaveGame.Gdd03Ruleset || o.DebtDollars is not double dollars) return null;
            var day = FillDate(e, date);
            return dollars * c.RulesAt(day).Mvp!.IlPerDollarOn(day);
        }

        /// <summary>A late-fired event can be resolved after a dated record's window has ended (chapters design 3.2): an
        /// effect on a target that no longer exists reaches nothing.</summary>
        static bool Reaches(GameState s, string target) => EventRules.SpecialTargets.Contains(target) || s.Indicators.ContainsKey(target);

        /// <summary>Where a grant stands (R-EFX-003, Q-HFX-2): the named city's settlement hex, or with no `at` the settlement
        /// hex of the one city with the military camp (the loader checks each exists).</summary>
        static (string Name, int Q, int R) GrantPlace(Content c, UnitGrant g)
        {
            var city = GrantCity(c, g);
            var hex = c.SettlementHexes.First(h => h.Id == city);   // every settlement has one (ValidateMap)
            return (PlayerText.Settlement(c, city), hex.Q, hex.R);
        }

        static string GrantCity(Content c, UnitGrant g) =>
            g.At != "" ? g.At : c.Cities.Single(x => x.Facilities.Contains(MilitaryCamp)).Id;

        static string Granted(UnitGrant g, string place) =>
            FormattableString.Invariant($"{g.N} {g.Type} unit{(g.N == 1 ? "" : "s")} at {place}");

        static string Unlocked(Content c, string kind) =>
            $"{PlayerText.Capitalised(c.ProjectKinds.First(k => k.Id == kind).Name)} can now be built";

        static string CityChange(Content c, string city, string field, double delta) =>
            field == ConditionField
                ? $"{PlayerText.Settlement(c, city)}: housing condition {PlayerText.PercentChange(delta)}"
                : FormattableString.Invariant($"{PlayerText.Settlement(c, city)}: {field.Replace('_', ' ')} {(delta > 0 ? "+" : "")}{delta}");

        /// <summary>R-EVT-005: every effect of an option, in the order choosing it applies them (Resolve): the numeric
        /// effects, the unit grants, city effects and project unlocks (R-EFX-006), the policies it sets (by id), the security
        /// level, then rejoin. Each amount is the option's own; Resolve may apply less where an indicator meets its bound or
        /// the Food or Arms stock its floor, and rounds both to whole units. The event card will read this list (Q-EVT-4,
        /// owner 17 October 2026; Phase 7 PR 8), so no number is computed in Unity.</summary>
        public static List<Cause> OptionEffects(Content c, GameState s, EventOption o) =>
            OptionEffectRows(c, s, o).Select(r => r.Cause).ToList();

        /// <summary>R-EVT-005: the same entries as <see cref="OptionEffects"/>, in the same order, each with read-only
        /// presentation context: its category, the place or row it names and its amount already formatted by the sim. A
        /// localized UI supplies nouns, not game rules. Both come from one walk of the option's effects, so they cannot drift.</summary>
        public static IReadOnlyList<OptionEffectPreview> OptionEffectPreviews(Content c, GameState s, EventOption o) =>
            OptionEffectRows(c, s, o).AsReadOnly();

        static List<OptionEffectPreview> OptionEffectRows(Content c, GameState s, EventOption o)
        {
            var rows = new List<OptionEffectPreview>();
            void Add(OptionEffectPreviewKind kind, string target, double delta, string message, string contextId, string contextName,
                string amount, string field = "")
            {
                var key = kind switch
                {
                    OptionEffectPreviewKind.Amount => CauseKeys.EventAmount,
                    OptionEffectPreviewKind.Stock => CauseKeys.EventStock,
                    OptionEffectPreviewKind.UnitGrant => CauseKeys.EventGrant,
                    OptionEffectPreviewKind.City => CauseKeys.EventCity,
                    OptionEffectPreviewKind.ProjectUnlock => CauseKeys.EventUnlock,
                    OptionEffectPreviewKind.SetPolicy => CauseKeys.EventPolicy,
                    OptionEffectPreviewKind.SetRung => CauseKeys.EventSetRung,
                    OptionEffectPreviewKind.Rejoin => CauseKeys.EventRejoin,
                    _ => throw new ArgumentOutOfRangeException(nameof(kind)),
                };
                var args = kind == OptionEffectPreviewKind.UnitGrant ? Context("", "", contextId)
                    : kind == OptionEffectPreviewKind.City ? Context("", "", contextId, field)
                    : kind == OptionEffectPreviewKind.Rejoin ? Context("", "", contextName,
                        SnapshotText(c, s, s.Date, c.Events.First(e => e.Id == contextId).Title, c.Events.First(e => e.Id == contextId).TitleHe))
                    : Context("", "");
                rows.Add(new OptionEffectPreview(new Cause("events", target, delta, message, key: key, args: args), kind, contextId, contextName, amount, field));
            }

            foreach (var fx in o.Effects.Where(fx => Reaches(s, fx.Target)))
            {
                var stock = fx.Target == Pools.FoodTarget || fx.Target == Pools.ArmsTarget;
                var name = stock ? (fx.Target == Pools.FoodTarget ? "Food" : "Arms") : PlayerText.Target(c, fx.Target);
                var amount = stock ? PlayerText.UnitsChangeNumber(fx.Delta)
                    : PlayerText.IsMoney(c, fx.Target) ? PlayerText.MoneyChange(c, fx.Target, fx.Delta) : PlayerText.PercentChange(fx.Delta);
                Add(stock ? OptionEffectPreviewKind.Stock : OptionEffectPreviewKind.Amount, fx.Target, fx.Delta,
                    PlayerText.Change(c, fx.Target, fx.Delta), fx.Target, name, amount);
            }
            if (c.Events.FirstOrDefault(e => e.Options.Contains(o)) is GameEvent owner && DebtFor(c, owner, o, s.Date) is double debt)
                Add(OptionEffectPreviewKind.Amount, "debt", debt, PlayerText.Change(c, "debt", debt), "debt", PlayerText.Target(c, "debt"),
                    PlayerText.MoneyChange(c, "debt", debt));
            foreach (var g in o.GrantUnits)
            {
                var place = GrantPlace(c, g).Name;
                Add(OptionEffectPreviewKind.UnitGrant, g.Type, g.N, Granted(g, place), GrantCity(c, g), place,
                    FormattableString.Invariant($"{g.N}"));
            }
            foreach (var x in o.CityEffects)
            {
                var amount = x.Field == ConditionField ? PlayerText.PercentChange(x.Delta)
                    : FormattableString.Invariant($"{(x.Delta > 0 ? "+" : "")}{x.Delta}");
                Add(OptionEffectPreviewKind.City, x.City + "." + x.Field, x.Delta, CityChange(c, x.City, x.Field, x.Delta),
                    x.City, PlayerText.Settlement(c, x.City), amount, x.Field);
            }
            foreach (var k in o.UnlockProjects)
                Add(OptionEffectPreviewKind.ProjectUnlock, k, 1, Unlocked(c, k), k,
                    PlayerText.Capitalised(c.ProjectKinds.First(x => x.Id == k).Name), "");
            foreach (var kv in o.SetPolicy.Where(k => s.Policies.ContainsKey(k.Key)).OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                var name = PlayerText.Target(c, kv.Key);
                var amount = PlayerText.Percent(kv.Value);
                Add(OptionEffectPreviewKind.SetPolicy, kv.Key, kv.Value, $"{name} set to {amount}", kv.Key, name, amount);
            }
            if (o.SetRung is int rung)
            {
                var amount = FormattableString.Invariant($"{rung}");
                Add(OptionEffectPreviewKind.SetRung, "rung", rung, $"Security level set to {amount}", "", "", amount);
            }
            // R-ALT-009: the way back to the record is shown with the other effects (R-EVT-005).
            if (o.Rejoin != "")
            {
                var title = Cabinet.Fill(c, s, c.Events.First(x => x.Id == o.Rejoin).Title, s.Date);
                Add(OptionEffectPreviewKind.Rejoin, "rejoin", 0, $"Returns to the recorded course: {title}", o.Rejoin, title, "");
            }
            return rows;
        }

        // #1680: presentation snapshots only. No parsing of English Message and no decision/RNG/state change.
        // Resolve content placeholders at the same historical date before later appointments can change their names.
        static string LocalName(Person p) => p.NameHe != "" ? p.NameHe : p.Name;
        static string SnapshotText(Content c, GameState s, DateTime date, string english, string hebrew) =>
            Cabinet.Fill(c, s, hebrew != "" ? hebrew : english, date, hebrew != "" ? LocalName : (Func<Person, string>?)null);
        static string HebrewTitle(Content c, GameState s, GameEvent e)
        {
            var variant = Timeline.Variant(c, s, e);
            return SnapshotText(c, s, FillDate(e, s.Date), variant != null && variant.Title != "" ? variant.Title : e.Title, TitleHe(c, s, e));
        }
        static string[] Context(string englishTitle, string hebrewTitle, params string[] values) =>
            new[] { englishTitle, hebrewTitle }.Concat(values).ToArray();

        /// <summary>The minister <see cref="Delegate"/> hands the event to in this state: its office's holder today, or null
        /// (no office, or nobody holds it: the cabinet decides). Simulation.DelegatedTo asks it after the queued orders.</summary>
        public static Person? DelegatedTo(Content c, GameState s, GameEvent e) => e.Office == "" ? null : Cabinet.Holder(c, s, e.Office);

        /// <summary>Unanswered events go to the responsible minister: with probability competence/10 they pick the
        /// recommended option (else the first), otherwise a random option. In Historical mode a hard-anchor event
        /// resolves to its historical option without drawing from the RNG (major anchors happen). An objective's card has one
        /// option and resolves to it without a draw (R-OBJ-003: GDD Phase 1 adds no RNG draw).
        /// In the GDD loop a drawn option of a minister whose loyalty is below the floor applies only disloyal_effect_share of
        /// each numeric effect (R-EVT-003; #107, owner, 18 October 2026). Why: loyalty decides how hard a minister works for a
        /// decision he was left to make, competence which decision he makes; anchors and objective cards stay whole.</summary>
        public static void Delegate(Content c, GameState s, List<Cause> why, Dictionary<int, int>? spent = null, List<Rejection>? rejected = null,
                                    List<OperationAction>? actions = null, Dictionary<int, AttackRecord>? attacked = null)
        {
            PopulationLedger.RefuseAggregate(s);
            var rng = new Rng(s.RngState);
            var mvp = c.IsGdd ? c.RulesAt(s.Date).Mvp : null;   // the legacy loop has no floor, so its goldens hold
            foreach (var id in s.PendingEvents.ToList())
            {
                var e = Get(c, id);
                if (LapseKey(c, s, e) is string lapse) { Lapse(c, s, e, why, lapse); continue; }
                var minister = DelegatedTo(c, s, e);
                if (e.Objective != "" || e.Prisoners != "" || e.Deported != "")   // R-POW-004, R-EVT-007: an exchange card or a Deport card, like an objective's, has one option and draws nothing
                {
                    s.RngState = rng.State;   // an operation's attack draws from the state (R-ENEMY-008), so hand it the stream and take it back
                    Resolve(c, s, id, e.Options[0], $"delegated to {minister?.Name ?? "the cabinet"}", why, delegated: true, minister: minister, spent: spent, rejected: rejected, actions: actions, attacked: attacked);
                    rng.State = s.RngState;
                    continue;
                }
                var beforeDraw = rng.State;
                var competence = (minister?.Competence ?? 5) / 10.0;
                var anchored = s.Mode == Mode.Historical && e.Anchor == "hard" ? e.Options.FirstOrDefault(o => o.Historical) : null;
                // R-ENEMY-007 (owner, 7 November 2026: "Keep the option (Recommended)"): a postponed card keeps the option it was
                // postponed with and draws nothing. Latrun's ruling ("Takes the vote, wait ends (Recommended)"): when the card's
                // historical option needs no units, its minister takes that one instead, and the wait ends.
                s.PostponedOperations.TryGetValue(id, out var postponed);
                var free = postponed == null ? null : e.Options.FirstOrDefault(o => o.Historical && o.RequiresUnitRegions.Count == 0);
                var kept = postponed == null ? null : e.Options.FirstOrDefault(o => o.Id == postponed.Option);
                var option = free ?? kept ?? anchored ?? (rng.NextDouble() < competence
                    ? e.Options.FirstOrDefault(o => o.Recommended) ?? e.Options[0]
                    : e.Options[(int)(rng.NextDouble() * e.Options.Count)]);
                var share = anchored == null && mvp != null && minister != null && minister.Loyalty < mvp.DelegationLoyaltyFloor
                    ? mvp.DisloyalEffectShare!.Value : 1.0;
                var how = $"delegated to {minister?.Name ?? "the cabinet"}" + (share < 1 ? ", carried out half-heartedly" : "");
                // Q3 ("Decide armistice first (Recommended)"): an operation waits while its front's armistice card is pending,
                // whichever comes first in the pending list; a signature then lapses it at the turn's end ("Signing wins").
                if (ArmisticeBlock(c, s, e, option) == RefusedOperationArmisticeFirst)
                {
                    rng.State = beforeDraw;
                    if (postponed == null) s.PostponedOperations[id] = new PostponedOperation(option.Id, s.Turn, s.Date);
                    why.Add(OperationCause(c, s, e, Array.Empty<OperationRequirementRow>(), CauseKeys.OperationWaitsForArmistice));
                    continue;
                }
                var status = OperationStatus(c, s, option);
                if (!status.Met)
                {
                    rng.State = beforeDraw;   // Q2: postponement consumes no draw
                    // R-ENEMY-007 (owner, 7 November 2026: "Lapse at once (Recommended)"): a required unit that is no longer on the
                    // board can never arrive, so the card lapses now rather than waiting out its turns.
                    if (Lost(c, s, option).Count > 0) { Lapse(c, s, e, why, CauseKeys.OperationLapsedUnitLost, option); continue; }
                    if (postponed == null) s.PostponedOperations[id] = new PostponedOperation(option.Id, s.Turn, s.Date);
                    why.Add(OperationCause(c, s, e, status.Missing, CauseKeys.OperationPostponed));
                    continue;
                }
                s.RngState = rng.State;   // as above: the attack's draw follows the minister's and is kept
                Resolve(c, s, id, option, how, why, share, delegated: true, minister: minister, spent: spent, rejected: rejected, actions: actions, attacked: attacked);
                rng.State = s.RngState;
            }
            s.RngState = rng.State;
        }

        /// <summary>R-ENEMY-007: at the turn's end, each pending operation card whose wait is over lapses without a choice or
        /// effects (<see cref="LapseKey"/>). Stable pending-list order; no RNG draw.</summary>
        public static void LapseExpiredOperations(Content c, GameState s, List<Cause> why)
        {
            foreach (var id in s.PendingEvents.ToList())
            {
                var e = Get(c, id);
                if (LapseKey(c, s, e) is string key) Lapse(c, s, e, why, key);
            }
        }

        /// <summary>R-ENEMY-007 (owner, 7 November 2026): why a pending operation card lapses now, or null while it may still
        /// come. Its front's signed armistice ends it ("Signing wins (Recommended)"); a postponed card lapses once a unit its
        /// kept option needs is no longer on the board ("Lapse at once (Recommended)"), or once it has been shown again on
        /// mvp.operation_wait_turns turns after its first postponement ("6 turns (Recommended)"); a card never postponed lapses
        /// at its window's end, as before. GDD only.</summary>
        internal static string? LapseKey(Content c, GameState s, GameEvent e)
        {
            if (!c.IsGdd || !e.Options.Any(o => o.RequiresUnitRegions.Count != 0)) return null;
            if (e.ArmisticeFront != "" && Demobilization.Signed(s, e.ArmisticeFront)) return CauseKeys.OperationLapsedSigned;
            if (s.PostponedOperations.TryGetValue(e.Id, out var p))
            {
                var kept = e.Options.FirstOrDefault(o => o.Id == p.Option);
                if (kept != null && Lost(c, s, kept).Count > 0) return CauseKeys.OperationLapsedUnitLost;
                return s.Turn - p.Turn > c.RulesAt(s.Date).Mvp!.OperationWaitTurns!.Value ? CauseKeys.OperationLapsed : null;
            }
            return e.To.HasValue && e.To.Value <= s.Date ? CauseKeys.OperationLapsed : null;
        }

        /// <summary>Q3 (owner, 7 November 2026: "Decide armistice first (Recommended)"): why the operation option <paramref name="o"/>
        /// of <paramref name="e"/> cannot go now because of its front's armistice (pending, or signed), or null. GDD only.</summary>
        public static string? ArmisticeBlock(Content c, GameState s, GameEvent e, EventOption o)
        {
            if (!c.IsGdd || o.RequiresUnitRegions.Count == 0 || e.ArmisticeFront == "") return null;
            if (Demobilization.Signed(s, e.ArmisticeFront)) return RefusedOperationArmisticeSigned;
            var card = "armistice_" + e.ArmisticeFront;
            return card != e.Id && s.PendingEvents.Contains(card) ? RefusedOperationArmisticeFirst : null;
        }

        /// <summary>R-ENEMY-007: the rows <paramref name="o"/> requires that are no longer on the board: their unit has arrived
        /// (a dated row is only waited for until its day, R-UNIT-009) and is gone, or its id now holds another unit.</summary>
        public static IReadOnlyList<string> Lost(Content c, GameState s, EventOption o) =>
            o.RequiresUnitRegions.Count == 0 ? Array.Empty<string>() : o.UnitOrders.Select(x => x.Unit).Distinct(StringComparer.Ordinal)
                .Where(row =>
                {
                    int id = c.StartingForces.FindIndex(f => f.Row == row);
                    if (id < 0) return false;
                    var force = c.StartingForces[id];
                    if (force.Arrives is DateTime day && s.Date < day) return false;
                    return !s.Units.TryGetValue(id, out var u) || u.Side != force.Side || u.Type != force.Type;
                })
                .OrderBy(x => x, StringComparer.Ordinal).ToList();

        static void Lapse(Content c, GameState s, GameEvent e, List<Cause> why, string key, EventOption? option = null)
        {
            s.PostponedOperations.TryGetValue(e.Id, out var p);
            option ??= p == null ? null : e.Options.FirstOrDefault(o => o.Id == p.Option);
            var operations = option != null ? new[] { option } : e.Options.Where(o => o.RequiresUnitRegions.Count != 0).ToArray();
            var rows = key == CauseKeys.OperationLapsedSigned ? Array.Empty<OperationRequirementRow>()
                : operations.SelectMany(o => key == CauseKeys.OperationLapsedUnitLost
                        ? OperationStatus(c, s, o).Required.Where(r => Lost(c, s, o).Contains(r.Unit))
                        : OperationStatus(c, s, o).Missing)
                    .GroupBy(x => x.Unit, StringComparer.Ordinal).Select(g => g.First()).OrderBy(x => x.Unit, StringComparer.Ordinal).ToArray();
            s.PendingEvents.Remove(e.Id);
            s.PostponedOperations.Remove(e.Id);
            why.Add(OperationCause(c, s, e, rows, key));
            // Q8 (owner, 7 November 2026: "All five -0.01 (Recommended)"): every lapse costs Horev's Hold in Security, where the
            // content has a Security indicator (as an event's effect reaches only indicators the state has).
            if (!Reaches(s, "security")) return;
            var title = Title(c, s, e);
            var change = EffectsSystem.Shift(c, s, "security", c.RulesAt(s.Date).Mvp!.OperationLapseSecurity!.Value);
            why.Add(new Cause("events", "security", change, $"{PlayerText.Change(c, "security", change)} ({title})",
                key: CauseKeys.EventAmount, args: Context(title, HebrewTitle(c, s, e))));
        }

        static Cause OperationCause(Content c, GameState s, GameEvent e, IReadOnlyList<OperationRequirementRow> rows, string key)
        {
            var title = Title(c, s, e); var titleHe = HebrewTitle(c, s, e);
            var args = new List<string> { title, titleHe };
            foreach (var row in rows.OrderBy(x => x.Unit, StringComparer.Ordinal))
            {
                args.Add(row.Unit);
                args.Add(string.Join("|", row.Regions.OrderBy(x => x, StringComparer.Ordinal)));
            }
            var moves = string.Join("; ", rows.OrderBy(x => x.Unit, StringComparer.Ordinal)
                .Select(x => OperationUnitName(c, x.Unit) + " to " + string.Join(" or ", x.Regions.OrderBy(r => r, StringComparer.Ordinal).Select(OperationRegionName))));
            var lost = string.Join(", ", rows.OrderBy(x => x.Unit, StringComparer.Ordinal).Select(x => OperationUnitName(c, x.Unit)));
            if (key == CauseKeys.OperationLapsedSigned || key == CauseKeys.OperationWaitsForArmistice) args.Add(e.ArmisticeFront);
            var message = key == CauseKeys.OperationLapsed
                ? $"{title} lapsed because the required units never reached the operation regions" + (moves == "" ? "" : $": {moves}")
                : key == CauseKeys.OperationLapsedSigned
                ? $"{title} lapsed because the armistice with {FrontName(e.ArmisticeFront)} was signed"
                : key == CauseKeys.OperationWaitsForArmistice
                ? $"{title} postponed until the armistice with {FrontName(e.ArmisticeFront)} is decided"
                : key == CauseKeys.OperationLapsedUnitLost
                ? $"{title} lapsed because a required unit is no longer on the board: {lost}"
                : $"{title} postponed; move {moves}";
            return new Cause("events", e.Id, 0, message, key: key, args: args);
        }

        static string OperationUnitName(Content c, string row)
        {
            var force = c.StartingForces.FirstOrDefault(x => x.Row == row);
            return force != null && force.Name != "" ? force.Name : row;
        }

        /// <summary>The sim's English name of a front (Prisoners.Fronts), as Unity's map.front_* strings give it.</summary>
        static string FrontName(string front) => front.Length == 0 ? front : char.ToUpperInvariant(front[0]) + front.Substring(1);

        /// <summary>English display names paired with Unity's operation.region.* catalogue. These five authored regions
        /// are the complete R-ENEMY-007 vocabulary; structured cause args retain the ids for EN/HE presentation lookup.</summary>
        internal static string OperationRegionName(string id) => id switch
        {
            "central_plain" => "Central Plain",
            "jerusalem_hills" => "Jerusalem Hills",
            "galilee" => "Galilee",
            "southern_coast" => "Southern Coast",
            "negev" => "Negev",
            _ => id,
        };

        public static string Render(Content c, GameEvent e, DateTime date) => Offices.Fill(c, e.Text, FillDate(e, date));

        /// <summary>The card's title as the player sees it now: a matching variant's, else the event's (R-ALT-010).</summary>
        public static string Title(Content c, GameState s, GameEvent e)
        {
            var v = Timeline.Variant(c, s, e);
            return Cabinet.Fill(c, s, v != null && v.Title != "" ? v.Title : e.Title, FillDate(e, s.Date));
        }

        /// <summary>Event text as the player sees it now: a matching variant's, with names following the player's appointments.</summary>
        public static string Render(Content c, GameState s, GameEvent e) =>
            Cabinet.Fill(c, s, Timeline.Variant(c, s, e)?.Text
                ?? (ShowsEarly(e, s.Date) ? e.EarlyText : ShowsLate(e, s.Date) ? e.LateText : e.Text), FillDate(e, s.Date));

        /// <summary>#958 (owner, 11 November 2026: "Add Hebrew support first (Recommended)"): the Hebrew of
        /// <see cref="Title(Content, GameState, GameEvent)"/>, unfilled, as title_he is: a matching variant's title_he when the
        /// variant has its own title, else the event's title_he. Empty where that title has no Hebrew; the UI then shows the
        /// English (#1064, fail open).</summary>
        public static string TitleHe(Content c, GameState s, GameEvent e)
        {
            var v = Timeline.Variant(c, s, e);
            return v != null && v.Title != "" ? v.TitleHe : e.TitleHe;
        }

        /// <summary>#958: the Hebrew of <see cref="Render(Content, GameState, GameEvent)"/>, unfilled: a matching variant's
        /// text_he, else the early, late or ordinary text_he, picked as Render picks the English. Empty where that text has no
        /// Hebrew (#1064, fail open).</summary>
        public static string TextHe(Content c, GameState s, GameEvent e)
        {
            var v = Timeline.Variant(c, s, e);
            return v != null ? v.TextHe : ShowsEarly(e, s.Date) ? e.EarlyTextHe : ShowsLate(e, s.Date) ? e.LateTextHe : e.TextHe;
        }

        /// <summary>R-ENEMY-007 ("Dateless variants (Recommended)"): whether <paramref name="e"/>, shown on <paramref name="date"/>,
        /// shows its late text: it has one, and its own window has closed. A road variant (R-ALT-010) still comes first.</summary>
        public static bool ShowsLate(GameEvent e, DateTime date) => e.LateText != "" && e.To.HasValue && date >= e.To.Value;

        /// <summary>R-WAR-008 (#1709; owner, 9 November 2026: "Separate early text (Recommended)", "Ordinary text
        /// (Recommended)"): whether <paramref name="e"/>, shown on <paramref name="date"/>, shows its early text: it has one, and
        /// it is shown before its `from`. The loader allows an early text only on a front_quiet card, which only the quiet path
        /// shows before its `from` (gdd-0.3, Armistice.MayOfferEarly), and a card is shown on the date it fired (it is answered
        /// or delegated before the date moves), so the date alone tells the early path: no saved state. A card refused and
        /// brought back on or after its `from` shows its ordinary text. A road variant (R-ALT-010) still comes first.</summary>
        public static bool ShowsEarly(GameEvent e, DateTime date) => e.EarlyText != "" && date < e.From;
    }
}
