using System;
using System.Collections.Generic;
using System.Globalization;

namespace Statecraft.Sim
{
    /// <summary>One line of the "Why did this happen?" report.</summary>
    public sealed class Cause
    {
        public string System { get; }
        public string Target { get; }
        public double Delta { get; }
        public string Message { get; }
        /// <summary>What the enemy did, when this cause records it (R-ENEMY-001): Movement.Kind or Orders.Fortify for the order
        /// the enemy AI gave unit Target, or Combat.Attack for an engagement its units led on hex Target (#1297); else empty.
        /// The turn report words it in the player's language (#1293), as Message is English only. A move or fortify the unit
        /// already has is kept with no cause (R-ENEMY-003), so it is named only on the turn it is given; an attack is named on
        /// every turn it engages, as a standing one still does.</summary>
        public string EnemyOrder { get; }
        /// <summary>With EnemyOrder Combat.Attack: the enemy units that attacked in the engagement, in id order; else empty.</summary>
        public IReadOnlyList<int> EnemyAttackers { get; }
        /// <summary>A stable localization key for a unit line (#1399) or report producer (CauseKeys); else empty.</summary>
        public string Key { get; }
        /// <summary>Snapshots of the units this line names, in placeholder order (#1399); else empty.</summary>
        public IReadOnlyList<UnitRef> Units { get; }
        /// <summary>The line's other placeholder values, as invariant text and in placeholder order (#1399); else empty.</summary>
        public IReadOnlyList<string> Args { get; }
        public Cause(string system, string target, double delta, string message, string enemyOrder = "", IReadOnlyList<int>? enemyAttackers = null,
            string key = "", IReadOnlyList<UnitRef>? units = null, IReadOnlyList<string>? args = null)
        {
            System = system; Target = target; Delta = delta; Message = PlayerText.Capitalised(message);   // sentence case (playtest 2, P2-6)
            EnemyOrder = enemyOrder; EnemyAttackers = enemyAttackers ?? Array.Empty<int>();
            Key = key; Units = units ?? Array.Empty<UnitRef>(); Args = args ?? Array.Empty<string>();
        }
        public override string ToString() => $"[{System}] {Message}";
    }

    /// <summary>B1: the keys of the resource, population, effect, security and remaining report lines (#1682), so the UI words them in the player's
    /// language (why.cause.&lt;key&gt;) from the line's Target, Delta and Args instead of its English Message. Message stays
    /// as it was, word for word, for the CLI, the tests and every line without a key. These lines name no unit, so they are
    /// apart from <see cref="WhyKeys"/>. A line that is not keyed yet keeps its English Message on screen.</summary>
    public static class CauseKeys
    {
        /// <summary>Target money moved by Delta. Args: tax, spending, reserve call-up (Economy).</summary>
        public const string MoneyFlows = "money_flows";
        /// <summary>Foreign currency moved by Delta. Args: exports, donations and loans, imports.</summary>
        public const string FxFlows = "fx_flows";
        /// <summary>Delta borrowed into the debt. Args: what for (a <see cref="Purpose"/>), then that purpose's values.</summary>
        public const string Borrowed = "borrowed";
        /// <summary>Target money moved by Delta, for civilian upkeep. No args.</summary>
        public const string CivilianUpkeep = "civilian_upkeep";
        public const string FxCrisisBegan = "fx_crisis_began", FxCrisisEased = "fx_crisis_eased";
        /// <summary>Target (food_supply) moved by Delta while the foreign currency crisis cuts food imports. No args.</summary>
        public const string FxCrisisFood = "fx_crisis_food";
        /// <summary>Delta immigrants arrived from the origin group Target. No args.</summary>
        public const string Arrived = "immigrants_arrived";
        /// <summary>Indicator Target moved by Delta. Args: the ids of the effects that moved it, in content order (none: no
        /// effect named).</summary>
        public const string EffectChange = "effect_change";
        /// <summary>Situation Target began or ended. Args: its indicator, the indicator's level.</summary>
        public const string SituationBegan = "situation_began", SituationEnded = "situation_ended";
        /// <summary>The security level changed. Args: from, to.</summary>
        public const string RungChanged = "rung_changed";
        /// <summary>The call-up was set to Delta. No args.</summary>
        public const string CallUpSet = "call_up_set";
        /// <summary>The call-up was raised or lowered to a level's minimum. Args: the new call-up, the security level.</summary>
        public const string CallUpRaised = "call_up_raised", CallUpLowered = "call_up_lowered";
        public const string Ceasefire = "ceasefire";

        /// <summary>Objective completed in the events group. Delta: points; Args: historical English/Hebrew name.</summary>
        public const string ObjectiveCompleted = "objective_completed";
        /// <summary>Prisoner exchange rows. Args: the displayed people count (invariant), then the front id.</summary>
        public const string PrisonersReturned = "prisoners_returned", SoldiersReturned = "soldiers_returned";

        // C136: event context Args begin with the shown English and sourced Hebrew title (empty for a preview).
        public const string EventNew = "event_new", EventChosen = "event_chosen", EventDelegated = "event_delegated",
            EventDelegatedPartial = "event_delegated_partial";
        // Chosen/delegated Args: filled English option, filled Hebrew option, English minister, Hebrew minister (empty: cabinet).
        public const string EventAmount = "event_amount", EventStock = "event_stock", EventGrant = "event_grant",
            EventCity = "event_city", EventUnlock = "event_unlock", EventPolicy = "event_policy",
            EventSetRung = "event_set_rung", EventRejoin = "event_rejoin";
        // Grant: context then city id. City: context then city id, field. Rejoin: context then EN/HE title.
        public const string MilitaryUpkeep = "military_upkeep";


        // #1682: immutable values in Args; no current-state lookup is needed to word an old report.
        /// <summary>Args: CauseKeys.Number(work).</summary>
        public const string WorkersAssigned = "workers_assigned";
        /// <summary>Args: none.</summary>
        public const string WagesPaid = "wages_paid";
        /// <summary>Args: CauseKeys.Number(absorbed).</summary>
        public const string ImmigrantsHoused = "immigrants_housed";
        /// <summary>Args: CauseKeys.Number(over).</summary>
        public const string HousingShortage = "housing_shortage";
        /// <summary>Args: CauseKeys.Number(added).</summary>
        public const string ImmigrantsWaiting = "immigrants_waiting";
        /// <summary>Args: none.</summary>
        public const string RecruitsVeteran = "recruits_veteran";
        /// <summary>Args: def.Id; CauseKeys.Number(n); CauseKeys.Number(cost).</summary>
        public const string CorridorStarted = "corridor_started";
        /// <summary>Args: def.Id; CauseKeys.Number(index + 1).</summary>
        public const string CorridorCancelled = "corridor_cancelled";
        /// <summary>Args: def.Id; CauseKeys.Number(i + 1).</summary>
        public const string CorridorCompleted = "corridor_completed";
        /// <summary>Args: none.</summary>
        public const string PioneersVeteran = "pioneers_veteran";
        /// <summary>Args: action.</summary>
        public const string MapBorrowed = "map_borrowed";
        /// <summary>Args: CauseKeys.Number(rule.Cost); CauseKeys.Number(housing); CauseKeys.Number(morale).</summary>
        public const string MapBuilt = "map_built";
        /// <summary>Args: plow ? "plow" : "drain"; CauseKeys.Number(cost).</summary>
        public const string LandWorkStarted = "land_work_started";
        /// <summary>Args: camp ? "camp" : "yishuv"; CauseKeys.Number(cost).</summary>
        public const string SiteStarted = "site_started";
        /// <summary>Args: none.</summary>
        public const string SiteCancelled = "site_cancelled";
        /// <summary>Args: IsCamp(place) ? "built" : place.Kinds.Contains(FieldKind) ? "plowed" : place.Kinds.Contains(SwampKind) ? "drained" : "founded".</summary>
        public const string SiteCompleted = "site_completed";
        /// <summary>Args: kind; CauseKeys.Number(k.Cost).</summary>
        public const string ProjectStarted = "project_started";
        /// <summary>Args: k.Id.</summary>
        public const string ProjectCancelled = "project_cancelled";
        /// <summary>Args: k.Id.</summary>
        public const string ProjectCompleted = "project_completed";
        /// <summary>Args: CauseKeys.Number(cost).</summary>
        public const string ArmsBought = "arms_bought";
        /// <summary>Args: f.Id.</summary>
        public const string FoodHarvest = "food_harvest";
        /// <summary>Args: CauseKeys.Number(need).</summary>
        public const string FoodShortage = "food_shortage";
        /// <summary>Args: CauseKeys.Number(s.Food).</summary>
        public const string FoodWithoutImports = "food_without_imports";
        /// <summary>Args: none.</summary>
        public const string PrisonerFood = "prisoner_food";
        /// <summary>Args: CauseKeys.Number(unfed).</summary>
        public const string PrisonerFoodShortage = "prisoner_food_shortage";
        /// <summary>Args: CauseKeys.Number(count).</summary>
        public const string CraftsmenReturned = "craftsmen_returned";
        /// <summary>Args: CauseKeys.Number(count).</summary>
        public const string CraftsmenReassigned = "craftsmen_reassigned";
        /// <summary>Args: city.</summary>
        public const string PioneerReturned = "pioneer_returned";
        /// <summary>Args: CauseKeys.Number(count); veteran ? "veteran" : "halutz".</summary>
        public const string PioneersReassigned = "pioneers_reassigned";
        /// <summary>Args: CauseKeys.Number(count); Training.CourseId(type); unit.HasValue ? CauseKeys.Number(unit.Value) : ""; CauseKeys.Number(cost).</summary>
        public const string TrainingStarted = "training_started";
        /// <summary>Args: city; Training.CourseId(type).</summary>
        public const string TrainingArms = "training_arms";
        /// <summary>Args: CauseKeys.Number(k.Count); Training.CourseId(type).</summary>
        public const string TrainingCancelled = "training_cancelled";
        /// <summary>Args: k.City; Training.CourseId(type).</summary>
        public const string TrainingArmsReturned = "training_arms_returned";
        /// <summary>Args: CauseKeys.Number(aim).</summary>
        public const string ReplacementsStopped = "replacements_stopped";
        /// <summary>Args: CauseKeys.Number(k.Count); Training.CourseId(type); CauseKeys.Number(target.Id).</summary>
        public const string ReplacementsJoined = "replacements_joined";
        /// <summary>Args: CauseKeys.Number(k.Count); Training.CourseId(type); type.Class == Combat.ArtilleryClass ? "artillery" : type.Class == Armour ? "armour" : "recruit".</summary>
        public const string MilitaryTrained = "military_trained";
        /// <summary>Args: CauseKeys.Number(k.Count); Training.CourseId(type).</summary>
        public const string PioneersTrained = "pioneers_trained";
        /// <summary>Args: CauseKeys.Number(k.Count); Training.CourseId(type).</summary>
        public const string CraftsmenTrained = "craftsmen_trained";
        /// <summary>Args: person.Id; office.TitleHeOn(person, s.Date); displaced?.Id ?? "".</summary>
        public const string Appointed = "appointed";
        /// <summary>Args: oldParty; displaced.Id; sameParty ? "same" : "other"; office.Title; office.TitleHeOn(displaced, s.Date).</summary>
        public const string Reshuffle = "reshuffle";
        /// <summary>Args: none.</summary>
        public const string AppointmentsEnded = "appointments_ended";
        /// <summary>Args: none.</summary>
        public const string ApprovalStability = "approval_stability";
        /// <summary>Args: none.</summary>
        public const string CoalitionCollapsed = "coalition_collapsed";
        /// <summary>Args: CauseKeys.Number(seats); CauseKeys.Number(size).</summary>
        public const string LegislatureConvened = "legislature_convened";
        /// <summary>Args: string.Join("|", g1!.CoalitionOn(g1.From)).</summary>
        public const string GovernmentTookOffice = "government_took_office";
        /// <summary>Args: string.Join("|", ch.Leaves); string.Join("|", ch.Joins); CauseKeys.Number(seats); CauseKeys.Number(size).</summary>
        public const string CoalitionChanged = "coalition_changed";
        /// <summary>Args: none.</summary>
        public const string EraEnded = "era_ended";
        /// <summary>Args: legislature.Id; CauseKeys.Number(p.Level).</summary>
        public const string AdvisoryPolicyRejected = "advisory_policy_rejected";
        /// <summary>Args: legislature.Id; CauseKeys.Number(p.Level).</summary>
        public const string PolicyRejected = "policy_rejected";
        /// <summary>Args: none.</summary>
        public const string PolicySet = "policy_set";
        /// <summary>Args: Offices.Legislature(c, s.Date)!.Id.</summary>
        public const string AdvisoryBudgetRejected = "advisory_budget_rejected";
        /// <summary>Args: Offices.Legislature(c, s.Date)!.Id.</summary>
        public const string BudgetRejected = "budget_rejected";
        /// <summary>Args: fled ? "1" : "0".</summary>
        public const string PlaceCaptured = "place_captured";
        /// <summary>Args: record.Fled ? "1" : "0".</summary>
        public const string EnemyRegained = "enemy_regained";
        /// <summary>Args: x.Fled ? "1" : "0".</summary>
        public const string PlaceAnnexed = "place_annexed";
        /// <summary>Args: moved.Count &gt; 0 ? "1" : "0"; residentsFled ? "1" : "0".</summary>
        public const string PlaceWithdrawn = "place_withdrawn";
        /// <summary>Args: none.</summary>
        public const string PlaceDeported = "place_deported";
        /// <summary>Args: id.</summary>
        public const string DeportEffect = "deport_effect";
        /// <summary>Args: id.</summary>
        public const string DeportCost = "deport_cost";
        /// <summary>Args: x.Fled ? "1" : "0".</summary>
        public const string OccupationStarted = "occupation_started";
        /// <summary>Args: none.</summary>
        public const string PendingBecameRaid = "pending_became_raid";
        /// <summary>Args: x.Fled ? "1" : "0".</summary>
        public const string OccupationLostUnit = "occupation_lost_unit";
        /// <summary>Args: none.</summary>
        public const string OccupationCountdown = "occupation_countdown";
        /// <summary>Args: none.</summary>
        public const string DepopulationSupport = "depopulation_support";

        /// <summary>What a <see cref="Borrowed"/> line borrowed for (EconomySystem.PayOrBorrow's callers, and the deficit).</summary>
        public static class Purpose
        {
            public const string Deficit = "deficit", CivilianUpkeep = "civilian_upkeep", MilitaryUpkeep = "military_upkeep",
                Corridor = "corridor", Arms = "arms", Training = "training", Prisoners = "prisoners", TrainingCamp = "training_camp",
                Plow = "plow", Drain = "drain", Yishuv = "yishuv", Deport = "deport", Occupation = "occupation";
            /// <summary>Args: the project kind's id.</summary>
            public const string Project = "project";
            /// <summary>Args: the event's id, its English title as shown, and "1" when its title_he names it (no variant title).</summary>
            public const string Event = "event";

            public static readonly IReadOnlyList<string> All = new[]
            {
                Deficit, CivilianUpkeep, MilitaryUpkeep, Corridor, Arms, Training, Prisoners, TrainingCamp, Plow, Drain, Yishuv,
                Deport, Occupation, Project, Event,
            };
        }

        public static readonly IReadOnlyList<string> All = new[]
        {
            MoneyFlows, FxFlows, Borrowed, CivilianUpkeep, FxCrisisBegan, FxCrisisEased, FxCrisisFood, Arrived, EffectChange,
            SituationBegan, SituationEnded, RungChanged, CallUpSet, CallUpRaised, CallUpLowered, Ceasefire,
            EventNew, EventChosen, EventDelegated, EventDelegatedPartial, EventAmount, EventStock, EventGrant, EventCity,
            EventUnlock, EventPolicy, EventSetRung, EventRejoin, MilitaryUpkeep,
            ObjectiveCompleted, PrisonersReturned, SoldiersReturned,
            WorkersAssigned, WagesPaid, ImmigrantsHoused, HousingShortage, ImmigrantsWaiting, RecruitsVeteran, CorridorStarted, CorridorCancelled, CorridorCompleted, PioneersVeteran, MapBorrowed, MapBuilt, LandWorkStarted, SiteStarted, SiteCancelled, SiteCompleted, ProjectStarted, ProjectCancelled, ProjectCompleted, ArmsBought, FoodHarvest, FoodShortage, FoodWithoutImports, PrisonerFood, PrisonerFoodShortage, CraftsmenReturned, CraftsmenReassigned, PioneerReturned, PioneersReassigned, TrainingStarted, TrainingArms, TrainingCancelled, TrainingArmsReturned, ReplacementsStopped, ReplacementsJoined, MilitaryTrained, PioneersTrained, CraftsmenTrained, Appointed, Reshuffle, AppointmentsEnded, ApprovalStability, CoalitionCollapsed, LegislatureConvened, GovernmentTookOffice, CoalitionChanged, EraEnded, AdvisoryPolicyRejected, PolicyRejected, PolicySet, AdvisoryBudgetRejected, BudgetRejected, PlaceCaptured, EnemyRegained, PlaceAnnexed, PlaceWithdrawn, PlaceDeported, DeportEffect, DeportCost, OccupationStarted, PendingBecameRaid, OccupationLostUnit, OccupationCountdown, DepopulationSupport,
        };

        /// <summary>A number as an Arg: invariant and round-trip, so the UI rounds exactly the value Message rounded.</summary>
        internal static string Number(double v) => v.ToString("R", CultureInfo.InvariantCulture);
        internal static string Number(int v) => v.ToString(CultureInfo.InvariantCulture);
    }
}
