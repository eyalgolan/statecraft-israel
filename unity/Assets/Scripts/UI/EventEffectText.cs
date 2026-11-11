#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Localized words around the sim's already formatted option-effect amounts. The event card shows these under each option (EventCardPresenter, #1125).</summary>
    public static class EventEffectText
    {
        /// <param name="contextName">Optional localized indicator, money, city, project kind, policy or event name from the caller.
        /// Empty or null uses the sim's English data name. Current model/city rows have no Hebrew name field.</param>
        public static string Format(OptionEffectPreview row, UiText text, string? contextName = null)
        {
            var name = string.IsNullOrEmpty(contextName) ? row.ContextName : contextName;
            switch (row.Kind)
            {
                case OptionEffectPreviewKind.Amount:
                    return text.Format("event.effect.amount", name, row.Amount);
                case OptionEffectPreviewKind.Stock:
                    if (string.IsNullOrEmpty(contextName))
                        name = text[row.Cause.Target switch
                        {
                            Pools.FoodTarget => "event.effect.food",
                            Pools.ArmsTarget => "event.effect.arms",
                            _ => throw new ArgumentOutOfRangeException(nameof(row), "Unknown stock presentation label"),
                        }];
                    return text.Format("event.effect.stock", name, row.Amount);
                case OptionEffectPreviewKind.UnitGrant:
                    var role = text[row.Cause.Target switch
                    {
                        "halutz" => "city.track_halutz",
                        "recruit" => "city.track_recruit",
                        "veteran_halutz" => "event.effect.veteran_halutz",
                        _ => "event.effect.units", // UnitType has no display name; never expose an unknown internal id.
                    }];
                    return text.Format("event.effect.grant", role, row.Amount, name);
                case OptionEffectPreviewKind.City:
                    var field = text[row.Field switch
                    {
                        EventSystem.HousingField => "event.effect.housing_capacity",
                        EventSystem.JobsField => "event.effect.job_slots",
                        EventSystem.ConditionField => "event.effect.condition",
                        _ => throw new ArgumentOutOfRangeException(nameof(row), "Unknown city-field presentation label"),
                    }];
                    return text.Format("event.effect.city", name, field, row.Amount);
                case OptionEffectPreviewKind.ProjectUnlock:
                    return text.Format("event.effect.unlock", name);
                case OptionEffectPreviewKind.UnitOrder:
                    return UnitOrder(row.Cause, text, null);
                case OptionEffectPreviewKind.SetPolicy:
                    return text.Format("event.effect.policy", name, row.Amount);
                case OptionEffectPreviewKind.SetRung:
                    return text.Format("event.effect.rung", row.Amount);
                case OptionEffectPreviewKind.Rejoin:
                    return text.Format("event.effect.rejoin", name);
                default:
                    throw new ArgumentOutOfRangeException(nameof(row), "Unknown effect presentation category");
            }
        }

        /// <summary>#2705, R-EVT-005: an operation card's unit order as given, never its outcome, from the cause's arguments
        /// (CauseKeys.EventUnitOrder: the row, the order, the unit's and the place's English names). In Hebrew the unit and the
        /// place take their Hebrew from <paramref name="units"/> (map/units_1948.yaml, which Unity reads itself): the row's
        /// name_he, and the place_he of its order target or anchor of that English name; the English where there is none.
        /// The Why panel words the same cause.</summary>
        public static string UnitOrder(Cause cause, UiText text, IReadOnlyList<UnitEntry>? units)
        {
            var a = cause.Args;
            var key = a[3] switch
            {
                Movement.GoTo => "event.effect.go_to",
                Combat.Attack => "event.effect.attack",
                _ => throw new ArgumentOutOfRangeException(nameof(cause), "Unknown unit-order presentation label"),
            };
            string unit = a[4], place = a[5];
            if (text["ui.lang"] == UiLanguage.Hebrew && units?.FirstOrDefault(x => x.Id == a[2]) is UnitEntry row)
            {
                if (!string.IsNullOrEmpty(row.NameHe)) unit = row.NameHe;
                var named = (row.OrderTargets ?? new List<UnitAnchor>()).Concat(row.Anchors).FirstOrDefault(x => x.Place == place);
                if (!string.IsNullOrEmpty(named?.PlaceHe)) place = named!.PlaceHe!;
            }
            return text.Format(key, unit, place);
        }
    }
}
