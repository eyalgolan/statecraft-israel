#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>B1: one Why line in the player's language, the one formatter the Why screen, the turn report and the meter
    /// tip share. A line with a CauseKeys key is worded from why.cause.&lt;key&gt; with its Target, Delta and Args, the
    /// numbers rounded by the same PlayerText rules as the sim's English Message, and the names it gives (indicator, money,
    /// effect, situation, origin group) from why.name, why.effect, why.situation and why.region; so the English line is the
    /// sim's Message, word for word. Every other line goes to <see cref="UnitNames.Line"/>: a unit line in the player's
    /// language, and a line with no key yet as the sim's English Message (not yet localized).</summary>
    public static class CauseText
    {
        public static string Line(Cause cause, Content c, IReadOnlyList<UnitEntry> rows, UiText text,
            IReadOnlyDictionary<string, string>? namesHe = null)
        {
            if (RemainingKeys.Contains(cause.Key)) return Remaining(cause, c, text, namesHe);
            var key = "why.cause." + cause.Key;
            switch (cause.Key)
            {
                case CauseKeys.MoneyFlows:
                case CauseKeys.FxFlows:
                    return text.Format(key, Name(text, cause.Target), Change(c, cause.Target, cause.Delta),
                        Money(c, cause.Target, cause.Args[0]), Money(c, cause.Target, cause.Args[1]), Money(c, cause.Target, cause.Args[2]));
                case CauseKeys.CivilianUpkeep:
                case CauseKeys.MilitaryUpkeep:
                case CauseKeys.FxCrisisFood:
                    return text.Format(key, Name(text, cause.Target), Change(c, cause.Target, cause.Delta));
                case CauseKeys.Borrowed:
                    return Borrowed(cause, c, text, namesHe);
                case CauseKeys.FxCrisisBegan:
                case CauseKeys.FxCrisisEased:
                case CauseKeys.Ceasefire:
                    return text[key];
                case CauseKeys.Arrived:
                    return text.Format(key, cause.Delta, text["why.region." + cause.Target]);
                case CauseKeys.EffectChange:
                    return cause.Args.Count == 0
                        ? text.Format(key, Name(text, cause.Target), Change(c, cause.Target, cause.Delta))
                        : text.Format(key + "_from", Name(text, cause.Target), Change(c, cause.Target, cause.Delta),
                            string.Join(", ", cause.Args.Select(id => text["why.effect." + id])));
                case CauseKeys.SituationBegan:
                case CauseKeys.SituationEnded:
                    return text.Format(key, text["why.situation." + cause.Target], Name(text, cause.Args[0]), PlayerText.Percent(Number(cause.Args[1])));
                case CauseKeys.RungChanged:
                    return text.Format(key, cause.Args[0], cause.Args[1]);
                case CauseKeys.CallUpSet:
                    return text.Format(key, PlayerText.Percent(cause.Delta));
                case CauseKeys.CallUpRaised:
                case CauseKeys.CallUpLowered:
                    return text.Format(key, PlayerText.Percent(Number(cause.Args[0])), cause.Args[1]);
                case CauseKeys.ObjectiveCompleted:
                    return PlayerText.Capitalised(text.Format(key, Local(cause.Args[0], cause.Args[1], text), (int)cause.Delta));
                case CauseKeys.PrisonersReturned:
                case CauseKeys.SoldiersReturned:
                    return text.Format(key, Number(cause.Args[0]), text["map.front_" + cause.Args[1]]);
                case CauseKeys.EventNew:
                    return text.Format(key, Local(cause.Args[0], cause.Args[1], text));
                case CauseKeys.EventChosen:
                    return text.Format(key, Local(cause.Args[0], cause.Args[1], text));
                case CauseKeys.EventDelegated:
                case CauseKeys.EventDelegatedPartial:
                    return text.Format(key, cause.Args[2] == "" ? text["why.cause.cabinet"] : Local(cause.Args[2], cause.Args[3], text),
                        Local(cause.Args[0], cause.Args[1], text));
                case CauseKeys.EventAmount:
                case CauseKeys.EventStock:
                case CauseKeys.EventGrant:
                case CauseKeys.EventCity:
                case CauseKeys.EventUnlock:
                case CauseKeys.EventPolicy:
                case CauseKeys.EventSetRung:
                case CauseKeys.EventRejoin:
                    return Event(cause, c, text, namesHe);
                default:
                    return UnitNames.Line(cause, c, rows, text, namesHe);
            }
        }

        // #1682: these branches use captured ids, values and resident flags, never the live GameState.
        static readonly HashSet<string> RemainingKeys = new HashSet<string>
        {
            CauseKeys.WorkersAssigned, CauseKeys.WagesPaid, CauseKeys.ImmigrantsHoused, CauseKeys.HousingShortage, CauseKeys.ImmigrantsWaiting, CauseKeys.RecruitsVeteran, CauseKeys.CorridorStarted, CauseKeys.CorridorCancelled, CauseKeys.CorridorCompleted, CauseKeys.PioneersVeteran, CauseKeys.MapBorrowed, CauseKeys.MapBuilt, CauseKeys.LandWorkStarted, CauseKeys.SiteStarted, CauseKeys.SiteCancelled, CauseKeys.SiteCompleted, CauseKeys.ProjectStarted, CauseKeys.ProjectCancelled, CauseKeys.ProjectCompleted, CauseKeys.ArmsBought, CauseKeys.FoodHarvest, CauseKeys.FoodShortage, CauseKeys.FoodWithoutImports, CauseKeys.PrisonerFood, CauseKeys.PrisonerFoodShortage, CauseKeys.CraftsmenReturned, CauseKeys.CraftsmenReassigned, CauseKeys.PioneerReturned, CauseKeys.PioneersReassigned, CauseKeys.TrainingStarted, CauseKeys.TrainingArms, CauseKeys.TrainingCancelled, CauseKeys.TrainingArmsReturned, CauseKeys.ReplacementsStopped, CauseKeys.ReplacementsJoined, CauseKeys.MilitaryTrained, CauseKeys.PioneersTrained, CauseKeys.CraftsmenTrained, CauseKeys.Appointed, CauseKeys.Reshuffle, CauseKeys.AppointmentsEnded, CauseKeys.ApprovalStability, CauseKeys.CoalitionCollapsed, CauseKeys.LegislatureConvened, CauseKeys.GovernmentTookOffice, CauseKeys.CoalitionChanged, CauseKeys.EraEnded, CauseKeys.AdvisoryPolicyRejected, CauseKeys.PolicyRejected, CauseKeys.PolicySet, CauseKeys.AdvisoryBudgetRejected, CauseKeys.BudgetRejected, CauseKeys.PlaceCaptured, CauseKeys.EnemyRegained, CauseKeys.PlaceAnnexed, CauseKeys.PlaceWithdrawn, CauseKeys.PlaceDeported, CauseKeys.DeportEffect, CauseKeys.DeportCost, CauseKeys.OccupationStarted, CauseKeys.PendingBecameRaid, CauseKeys.OccupationLostUnit, CauseKeys.OccupationCountdown, CauseKeys.DepopulationSupport
        };

        static string Remaining(Cause cause, Content c, UiText text, IReadOnlyDictionary<string, string>? namesHe)
        {
            string A(int i) => cause.Args[i];
            string Pick(string en, string he) => text["ui.lang"] == UiLanguage.Hebrew && he != "" ? he : en;
            string Settlement(string id) => ContentNames.Settlement(text, c, id, namesHe);
            string Site(string id) { var x = c.Sites.First(s => s.Id == id); return Pick(x.Name, x.NameHe); }
            string Groups(string count) => text["ui.lang"] == UiLanguage.Hebrew ? count
                : count == "1" ? "1 group" : count + " groups";
            string Course(string id) => text["why.cause.course." + id];
            string Residents(string fled) => text["why.cause.residents." + fled];
            string Parties(string ids) => string.Join(", ", ids.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => ContentNames.Of(text, c.Parties.First(x => x.Id == id))));
            string Coalition(string leaves, string joins) => string.Join("; ", new[] {
                leaves == "" ? "" : text.Format("why.cause.parties_left", Parties(leaves)),
                joins == "" ? "" : text.Format("why.cause.parties_joined", Parties(joins)) }.Where(x => x != ""));
            string Segment(string id, string number) { var x = c.Corridors.First(s => s.Id == id);
                return text.Format("why.cause.segment", Pick(x.Name, x.NameHe), number); }
            string Elected(Legislature x) => x.Elected.HasValue
                ? text.Format("why.cause.elected", text.Format("date.day_month_year", x.Elected.Value.Day, text["date.month_" + x.Elected.Value.Month.ToString(CultureInfo.InvariantCulture)], x.Elected.Value.Year)) : "";
            var key = "why.cause." + cause.Key;
            switch (cause.Key)
            {
                case CauseKeys.WorkersAssigned:
                    return text.Format(key, Settlement(cause.Target), A(0));
                case CauseKeys.WagesPaid:
                    return text.Format(key, Settlement(cause.Target), PlayerText.MoneyChange(c, "treasury", cause.Delta));
                case CauseKeys.ImmigrantsHoused:
                    return text.Format(key, Settlement(cause.Target), A(0));
                case CauseKeys.HousingShortage:
                    return text.Format(key, Settlement(cause.Target), A(0));
                case CauseKeys.ImmigrantsWaiting:
                    return text.Format(key, Settlement(cause.Target), A(0));
                case CauseKeys.RecruitsVeteran:
                    return text.Format(key);
                case CauseKeys.CorridorStarted:
                    return text.Format(key, Segment(A(0), A(1)), Money(c, "treasury", A(2)));
                case CauseKeys.CorridorCancelled:
                    return text.Format(key, Segment(A(0), A(1)));
                case CauseKeys.CorridorCompleted:
                    return text.Format(key, Segment(A(0), A(1)));
                case CauseKeys.PioneersVeteran:
                    return text.Format(key);
                case CauseKeys.MapBorrowed:
                    return text.Format(key, PlayerText.Money(c, "debt", cause.Delta), text["info.map_action." + A(0)]);
                case CauseKeys.MapBuilt:
                    return text.Format(key, text["info.map_action." + cause.Target], Money(c, "treasury", A(0)), Name(text, "housing"), Change(c, "housing", Number(A(1))), Name(text, "morale"), Change(c, "morale", Number(A(2))));
                case CauseKeys.LandWorkStarted:
                    return text.Format(key, Site(cause.Target), text["why.cause.work." + A(0)], Money(c, "treasury", A(1)));
                case CauseKeys.SiteStarted:
                    return text.Format(key, Site(cause.Target), text["why.cause.work." + A(0)], Money(c, "treasury", A(1)));
                case CauseKeys.SiteCancelled:
                    return text.Format(key, Site(cause.Target));
                case CauseKeys.SiteCompleted:
                    return text.Format(key, Site(cause.Target), text["why.cause.work." + A(0)]);
                case CauseKeys.ProjectStarted:
                    return text.Format(key, Settlement(cause.Target), ContentNames.Of(text, c.ProjectKinds.First(x => x.Id == A(0))), Money(c, "treasury", A(1)));
                case CauseKeys.ProjectCancelled:
                    return text.Format(key, Settlement(cause.Target), ContentNames.Of(text, c.ProjectKinds.First(x => x.Id == A(0))));
                case CauseKeys.ProjectCompleted:
                    return text.Format(key, Settlement(cause.Target), ContentNames.Of(text, c.ProjectKinds.First(x => x.Id == A(0))));
                case CauseKeys.ArmsBought:
                    return text.Format(key, cause.Delta, Money(c, "treasury", A(0)));
                case CauseKeys.FoodHarvest:
                    return text.Format(key, Site(A(0)), cause.Delta);
                case CauseKeys.FoodShortage:
                    return text.Format(key, -cause.Delta, A(0));
                case CauseKeys.FoodWithoutImports:
                    return text.Format(key, A(0));
                case CauseKeys.PrisonerFood:
                    return text.Format(key, -cause.Delta);
                case CauseKeys.PrisonerFoodShortage:
                    return text.Format(key, A(0), Name(text, cause.Target), Change(c, cause.Target, cause.Delta));
                case CauseKeys.CraftsmenReturned:
                    return text.Format(key, Settlement(cause.Target), Groups(A(0)));
                case CauseKeys.CraftsmenReassigned:
                    return text.Format(key, Settlement(cause.Target), Groups(A(0)));
                case CauseKeys.PioneerReturned:
                    return text.Format(key, cause.Target, Settlement(A(0)));
                case CauseKeys.PioneersReassigned:
                    return text.Format(key, Settlement(cause.Target), Groups(A(0)), text["why.cause.trained." + A(1)]);
                case CauseKeys.TrainingStarted:
                    return text.Format(key, Settlement(cause.Target), Groups(A(0)), Course(A(1)), A(2) == "" ? "" : text.Format("why.cause.replacements", A(2)), Money(c, "treasury", A(3)));
                case CauseKeys.TrainingArms:
                    return text.Format(key, Settlement(A(0)), Course(A(1)), -cause.Delta);
                case CauseKeys.TrainingCancelled:
                    return text.Format(key, Settlement(cause.Target), Groups(A(0)), Course(A(1)));
                case CauseKeys.TrainingArmsReturned:
                    return text.Format(key, Settlement(A(0)), Course(A(1)), cause.Delta);
                case CauseKeys.ReplacementsStopped:
                    return text.Format(key, Settlement(cause.Target), A(0));
                case CauseKeys.ReplacementsJoined:
                    return text.Format(key, Settlement(cause.Target), Groups(A(0)), Course(A(1)), A(2));
                case CauseKeys.MilitaryTrained:
                    return text.Format(key, Settlement(cause.Target), Groups(A(0)), Course(A(1)), text["why.cause.trained." + A(2)]);
                case CauseKeys.PioneersTrained:
                    return text.Format(key, Settlement(cause.Target), Groups(A(0)), Course(A(1)));
                case CauseKeys.CraftsmenTrained:
                    return text.Format(key, Settlement(cause.Target), Groups(A(0)), Course(A(1)));
                case CauseKeys.Appointed:
                    return text.Format(key, ContentNames.Of(text, c.People.First(x => x.Id == A(0))), Pick(c.Offices.First(x => x.Id == cause.Target).Title, A(1)), A(2) == "" ? "" : text.Format("why.cause.replacing", ContentNames.Of(text, c.People.First(x => x.Id == A(2)))));
                case CauseKeys.Reshuffle:
                    return text.Format(key, Change(c, cause.Target, cause.Delta), A(0) == "" ? ContentNames.Of(text, c.People.First(x => x.Id == A(1))) : ContentNames.Of(text, c.Parties.First(x => x.Id == A(0))), A(2) == "same" ? text["why.cause.resented"] : text.Format("why.cause.lost_office", Pick(A(3), A(4))));
                case CauseKeys.AppointmentsEnded:
                    return text.Format(key);
                case CauseKeys.ApprovalStability:
                    return text.Format(key, Change(c, cause.Target, cause.Delta));
                case CauseKeys.CoalitionCollapsed:
                    return text.Format(key);
                case CauseKeys.LegislatureConvened:
                    return text.Format(key, ContentNames.Of(text, c.Legislatures.First(x => x.Id == cause.Target)), c.Legislatures.First(x => x.Id == cause.Target).Size, Elected(c.Legislatures.First(x => x.Id == cause.Target)), A(0), A(1));
                case CauseKeys.GovernmentTookOffice:
                    return text.Format(key, ContentNames.Of(text, c.Governments.First(x => x.Id == cause.Target)), Parties(A(0)));
                case CauseKeys.CoalitionChanged:
                    return text.Format(key, Coalition(A(0), A(1)), A(2), A(3));
                case CauseKeys.EraEnded:
                    return text.Format(key, text["why.cause.ending." + cause.Target]);
                case CauseKeys.AdvisoryPolicyRejected:
                    return text.Format(key, ContentNames.Of(text, c.Legislatures.First(x => x.Id == A(0))), Name(text, cause.Target), PlayerText.Percent(Number(A(1))));
                case CauseKeys.PolicyRejected:
                    return text.Format(key, ContentNames.Of(text, c.Legislatures.First(x => x.Id == A(0))), Name(text, cause.Target), PlayerText.Percent(Number(A(1))));
                case CauseKeys.PolicySet:
                    return text.Format(key, Name(text, cause.Target), PlayerText.Percent(cause.Delta));
                case CauseKeys.AdvisoryBudgetRejected:
                    return text.Format(key, ContentNames.Of(text, c.Legislatures.First(x => x.Id == A(0))));
                case CauseKeys.BudgetRejected:
                    return text.Format(key, ContentNames.Of(text, c.Legislatures.First(x => x.Id == A(0))));
                case CauseKeys.PlaceCaptured:
                    return text.Format(key, Settlement(cause.Target), A(0) == "1" ? text["why.cause.fled_capture"] : "");
                case CauseKeys.EnemyRegained:
                    return text.Format(key, Settlement(cause.Target), A(0) == "1" ? ". " + Residents(A(0)) : "");
                case CauseKeys.PlaceAnnexed:
                    return text.Format(key, Settlement(cause.Target), text["why.cause.annexed." + A(0)]);
                case CauseKeys.PlaceWithdrawn:
                    return text.Format(key, Settlement(cause.Target), text["why.cause.withdrew." + A(0)], Residents(A(1)));
                case CauseKeys.PlaceDeported:
                    return text.Format(key, Settlement(cause.Target));
                case CauseKeys.DeportEffect:
                    return text.Format(key, Name(text, cause.Target), Change(c, cause.Target, cause.Delta), Settlement(A(0)));
                case CauseKeys.DeportCost:
                    return text.Format(key, Name(text, cause.Target), Change(c, cause.Target, cause.Delta), Settlement(A(0)));
                case CauseKeys.OccupationStarted:
                    return text.Format(key, Settlement(cause.Target), cause.Delta, Residents(A(0)));
                case CauseKeys.PendingBecameRaid:
                    return text.Format(key, Settlement(cause.Target));
                case CauseKeys.OccupationLostUnit:
                    return text.Format(key, Settlement(cause.Target), Residents(A(0)));
                case CauseKeys.OccupationCountdown:
                    if (cause.Delta == 0) return text.Format(key + "_ended", Settlement(cause.Target));
                    return text.Format(key, Settlement(cause.Target), cause.Delta);
                case CauseKeys.DepopulationSupport:
                    return text.Format(key, Name(text, cause.Target), Change(c, cause.Target, cause.Delta));
                default: throw new InvalidOperationException("Unknown remaining Cause key: " + cause.Key);
            }
        }

        static string Local(string english, string hebrew, UiText text) =>
            text["ui.lang"] == UiLanguage.Hebrew && hebrew != "" ? hebrew : english;

        /// <summary>C136: event changes and previews share keys; only an applied change has a title context. The sim
        /// already supplied its applied Delta and historical text snapshots; this formats no game rule or outcome.</summary>
        static string Event(Cause cause, Content c, UiText text, IReadOnlyDictionary<string, string>? namesHe)
        {
            var key = "why.cause." + cause.Key;
            object[] values;
            switch (cause.Key)
            {
                case CauseKeys.EventAmount:
                    values = new object[] { Name(text, cause.Target), Change(c, cause.Target, cause.Delta) };
                    break;
                case CauseKeys.EventStock:
                    values = new object[] { text[cause.Target == Pools.FoodTarget ? "event.effect.food" : "event.effect.arms"], PlayerText.UnitsChangeNumber(cause.Delta) };
                    break;
                case CauseKeys.EventGrant:
                    var role = text["ui.lang"] != UiLanguage.Hebrew ? cause.Target
                        : text.Has("unit.name." + cause.Target) ? text["unit.name." + cause.Target] : text["event.effect.units"];
                    key += cause.Delta == 1 ? "_one" : "_many";
                    values = new object[] { cause.Delta, role, ContentNames.Settlement(text, c, cause.Args[2], namesHe) };
                    break;
                case CauseKeys.EventCity:
                    var field = cause.Args[3];
                    values = new object[] { ContentNames.Settlement(text, c, cause.Args[2], namesHe),
                        text["event.effect." + field], field == EventSystem.ConditionField
                            ? PlayerText.PercentChange(cause.Delta) : PlayerText.UnitsChangeNumber(cause.Delta) };
                    break;
                case CauseKeys.EventUnlock:
                    values = new object[] { PlayerText.Capitalised(ContentNames.Of(text, c.ProjectKinds.First(k => k.Id == cause.Target))) };
                    break;
                case CauseKeys.EventPolicy:
                    values = new object[] { Name(text, cause.Target), PlayerText.Percent(cause.Delta) };
                    break;
                case CauseKeys.EventSetRung:
                    values = new object[] { cause.Delta };
                    break;
                case CauseKeys.EventRejoin:
                    values = new object[] { Local(cause.Args[2], cause.Args[3], text) };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cause));
            }
            if (cause.Args[0] != "")
            {
                key += "_from";
                values = values.Concat(new object[] { Local(cause.Args[0], cause.Args[1], text) }).ToArray();
            }
            return PlayerText.Capitalised(text.Format(key, values));
        }

        /// <summary>A borrowing line, by its purpose (CauseKeys.Purpose): why.cause.borrowed.&lt;purpose&gt;, with what the
        /// purpose names in the UI's language (a project kind, a settlement, an event's title).</summary>
        static string Borrowed(Cause cause, Content c, UiText text, IReadOnlyDictionary<string, string>? namesHe)
        {
            var key = "why.cause.borrowed." + cause.Args[0];
            var amount = PlayerText.Money(c, cause.Target, cause.Delta);
            switch (cause.Args[0])
            {
                case CauseKeys.Purpose.Project:
                    return text.Format(key, amount, ContentNames.Of(text, c.ProjectKinds.First(k => k.Id == cause.Args[1])));
                case CauseKeys.Purpose.Deport:
                case CauseKeys.Purpose.Occupation:
                    return text.Format(key, amount, ContentNames.Settlement(text, c, cause.Args[1], namesHe));
                case CauseKeys.Purpose.Event:
                    // The title as the card shows it: title_he, unless a variant gives its own (English) title.
                    var he = cause.Args[3] == "1" ? c.Events.First(e => e.Id == cause.Args[1]).TitleHe : "";
                    return text.Format(key, amount, he != "" && text["ui.lang"] == UiLanguage.Hebrew ? he : cause.Args[2]);
                default:
                    return text.Format(key, amount);
            }
        }

        /// <summary>An indicator's or a money's name (model.yaml's name in English).</summary>
        static string Name(UiText text, string id) => text["why.name." + id];

        /// <summary>The change in the target's unit, as PlayerText.Change words it after the name.</summary>
        static string Change(Content c, string id, double delta) =>
            PlayerText.IsMoney(c, id) ? PlayerText.MoneyChange(c, id, delta) : PlayerText.PercentChange(delta);

        static string Money(Content c, string id, string value) => PlayerText.Money(c, id, Number(value));

        static double Number(string value) => double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
