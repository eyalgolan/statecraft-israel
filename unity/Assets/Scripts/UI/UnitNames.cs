#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>#1399: names a unit snapshot and formats the structured Why lines that contain one. A starting force is
    /// named by its units_1948.yaml row (name_he in Hebrew where the row has one); a raised unit by its type and home city
    /// (unit.name.&lt;type&gt;: one formation, singular, as the lines' verbs are, never the type's plural label; the city's
    /// Hebrew name from <c>namesHe</c>, the map's parse, as the unit card does).</summary>
    public static class UnitNames
    {
        public static string Of(UnitRef unit, Content c, IReadOnlyList<UnitEntry> rows, UiText text,
            IReadOnlyDictionary<string, string>? namesHe = null)
        {
            if (unit.Id >= 0 && unit.Id < c.StartingForces.Count)
            {
                var force = c.StartingForces[unit.Id];
                if (force.Type == unit.Type && force.Side == unit.Side)
                {
                    var row = rows.FirstOrDefault(x => x.Id == force.Row);
                    if (row != null)
                        return text["ui.lang"] == UiLanguage.Hebrew && !string.IsNullOrEmpty(row.NameHe) ? row.NameHe : row.Name;
                }
            }

            var type = text.Has("unit.name." + unit.Type) ? text["unit.name." + unit.Type] : unit.Type;
            return string.IsNullOrEmpty(unit.HomeCity)
                ? type
                : text.Format("unit.raised_name", type, ContentNames.Settlement(text, c, unit.HomeCity, namesHe));
        }

        public static string Line(Cause cause, Content c, IReadOnlyList<UnitEntry> rows, UiText text,
            IReadOnlyDictionary<string, string>? namesHe = null)
        {
            if (string.IsNullOrEmpty(cause.Key)) return cause.Message;
            if (cause.Key == WhyKeys.Engagement)
            {
                var attackers = int.Parse(cause.Args[4], CultureInfo.InvariantCulture);
                return text.Format("why.line." + cause.Key,
                    string.Join(", ", cause.Units.Take(attackers).Select(u => Of(u, c, rows, text, namesHe))),
                    string.Join(", ", cause.Units.Skip(attackers).Select(u => Of(u, c, rows, text, namesHe))),
                    cause.Args[0], Number(cause.Args[1]), Number(cause.Args[2]), Number(cause.Args[3]));
            }

            var args = cause.Args.ToArray();
            if (cause.Key == WhyKeys.SentHome && args.Length > 1)
                args[1] = ContentNames.Settlement(text, c, args[1], namesHe);
            else if (cause.Key == WhyKeys.Withdrew && args.Length > 0)
                args[0] = ContentNames.Settlement(text, c, args[0], namesHe);
            var values = cause.Units.Select(u => (object)Of(u, c, rows, text, namesHe))
                .Concat(args.Select(Number)).ToArray();
            return text.Format("why.line." + cause.Key, values);
        }

        static object Number(string value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : value;
    }
}
