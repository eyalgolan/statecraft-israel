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
            var key = "why.cause." + cause.Key;
            switch (cause.Key)
            {
                case CauseKeys.MoneyFlows:
                case CauseKeys.FxFlows:
                    return text.Format(key, Name(text, cause.Target), Change(c, cause.Target, cause.Delta),
                        Money(c, cause.Target, cause.Args[0]), Money(c, cause.Target, cause.Args[1]), Money(c, cause.Target, cause.Args[2]));
                case CauseKeys.CivilianUpkeep:
                case CauseKeys.FxCrisisFood:
                    return text.Format(key, Name(text, cause.Target), Change(c, cause.Target, cause.Delta));
                case CauseKeys.Borrowed:
                    return text.Format(key + "." + cause.Args[0], PlayerText.Money(c, cause.Target, cause.Delta));
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
                default:
                    return UnitNames.Line(cause, c, rows, text, namesHe);
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
