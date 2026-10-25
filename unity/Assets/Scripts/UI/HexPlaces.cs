#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>A hex as the unit lines name it (#1527's follow-up): never its coordinates (no debug coordinates in player
    /// text, playtest 2, P2-6). A settlement's hex is the settlement's name; any other hex is said from the nearest
    /// settlement (map/settlement_hexes.yaml, the first in file order on a tie), how far and which way, so a hex far out in
    /// the Negev is not "near" anything.</summary>
    public static class HexPlaces
    {
        /// <summary>The sim's "q,r" (Movement.HexText and the like) as a place; any other value unchanged.</summary>
        public static string Of(string value, Content c, UiText text, IReadOnlyDictionary<string, string>? namesHe)
        {
            var parts = value.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var q)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r)) return value;
            SettlementHex? nearest = null; var best = int.MaxValue;
            foreach (var s in c.SettlementHexes)
            {
                var d = MapActions.Distance(q, r, s.Q, s.R);
                if (d < best) { best = d; nearest = s; }
            }
            if (nearest == null || !(c.HexAt(q, r) is MapHex to) || !(c.HexAt(nearest.Q, nearest.R) is MapHex from)) return value;
            var name = ContentNames.Settlement(text, c, nearest.Id, namesHe);
            if (best == 0) return name;
            // Between the two hexes' centres, on a local flat projection (a few dozen km at most), to the nearest 5 km.
            var east = (to.Lon - from.Lon) * Math.Cos((to.Lat + from.Lat) / 2 * Math.PI / 180) * 111.32;
            var north = (to.Lat - from.Lat) * 110.57;
            var km = Math.Max(5, (int)Math.Round(Math.Sqrt(east * east + north * north) / 5, MidpointRounding.AwayFromZero) * 5);
            var way = Directions[(int)Math.Round((Math.Atan2(east, north) * 180 / Math.PI + 360) % 360 / 45) % 8];
            return text.Format("why.hex.away", km, text["why.hex." + way], name);
        }

        /// <summary>The eight compass points, clockwise from north (why.hex.&lt;point&gt;).</summary>
        static readonly string[] Directions = { "north", "north_east", "east", "south_east", "south", "south_west", "west", "north_west" };
    }
}
