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
                case OptionEffectPreviewKind.UnitOrder:   // its Hebrew needs the content and the units catalogue
                    throw new ArgumentException("A unit order is worded by UnitOrder", nameof(row));
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
        /// (CauseKeys.EventUnitOrder: the row, the order, the unit's and the place's English names, the unit's type, the hex).
        /// In Hebrew the unit and the place take their Hebrew from <paramref name="units"/> (map/units_1948.yaml, which Unity
        /// reads itself): the row's name_he, and the place_he of its order target or anchor of that English name. Where there
        /// is none, the owner's "Generic Hebrew term (Recommended)" (11 November 2026), until a name list exists: the unit is
        /// named by its type (unit.name.&lt;type&gt;, as UnitNames names a raised unit) and the place by its hex (HexPlaces).
        /// The Why panel words the same cause.</summary>
        public static string UnitOrder(Cause cause, Content c, UiText text, IReadOnlyList<UnitEntry>? units,
            IReadOnlyDictionary<string, string>? namesHe = null)
        {
            var a = cause.Args;
            var key = a[3] switch
            {
                Movement.GoTo => "event.effect.go_to",
                Combat.Attack => "event.effect.attack",
                _ => throw new ArgumentOutOfRangeException(nameof(cause), "Unknown unit-order presentation label"),
            };
            string unit = a[4], place = a[5];
            if (text["ui.lang"] != UiLanguage.Hebrew) return text.Format(key, unit, place);
            var row = units?.FirstOrDefault(x => x.Id == a[2]);
            var named = row == null ? null : (row.OrderTargets ?? new List<UnitAnchor>()).Concat(row.Anchors).FirstOrDefault(x => x.Place == place);
            var type = "unit.name." + a[6];
            unit = !string.IsNullOrEmpty(row?.NameHe) ? row!.NameHe : text.Has(type) ? text[type] : unit;
            place = !string.IsNullOrEmpty(named?.PlaceHe) ? named!.PlaceHe! : HexPlaces.Of(a[7], c, text, namesHe);
            return text.Format(key, unit, place);
        }
    }
}
