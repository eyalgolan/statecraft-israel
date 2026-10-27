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
