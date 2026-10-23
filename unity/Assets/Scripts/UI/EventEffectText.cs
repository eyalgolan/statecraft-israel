#nullable enable
using System;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Localized words around the sim's already formatted option-effect amounts. Not yet wired to the card.</summary>
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
    }
}
