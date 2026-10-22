using System;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Player-facing wording shared by the sim (Cause text, refusal reasons) and the UI: the one date format
    /// ("15 May 1948"), the display names content gives its ids, money with its currency ("IL 18.6m", "$41.5m"), and the
    /// one scale for indicators (0-100, as the top bar shows them, written with "%"). No player-facing string shows a
    /// content id, an ISO date or a bare number (spec: player-facing text; playtest 1 items 4, 5 and 7). An id content
    /// does not name is an error, never shown as it is.</summary>
    public static class PlayerText
    {
        /// <summary>The money the sim tracks; model.yaml money names each one.</summary>
        public static readonly string[] MoneyIds = { "treasury", "debt", "fx_reserves" };

        /// <summary>The player-facing reason for a <see cref="GameState.GameOver"/> code (see <see cref="GameOverCodes"/>):
        /// tests and goldens compare the code, the player reads this.</summary>
        public static string GameOver(string code) => code switch
        {
            GameOverCodes.EraEnded => "The chapter has ended",
            GameOverCodes.MvpEnded => "The game has reached its last turn",
            GameOverCodes.CoalitionCollapse => "The coalition collapsed",
            // The short factual line per cause (Q-DEF-4 (b), owner, 18 Oct 2026, #784). The sourced history note
            // beside each cause goes on the end screen with #812.
            GameOverCodes.DefeatEconomy => "Money and Food ran out",
            GameOverCodes.DefeatWar => "The enemy holds a city",
            _ => throw Unknown("game over reason", code),
        };

        /// <summary>"15 May 1948".</summary>
        public static string Date(DateTime d) => d.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

        /// <summary>"May 1948".</summary>
        public static string Month(DateTime d) => d.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

        static InvalidOperationException Unknown(string what, string id) =>
            new InvalidOperationException($"no display name for {what} '{id}' (content validation should have caught this)");

        /// <summary>The display name of an aliyah origin group (aliyah.yaml regions).</summary>
        public static string Region(Content c, string id) => c.Regions.FirstOrDefault(r => r.Id == id)?.Name ?? throw Unknown("region", id);

        /// <summary>The display name of a settlement (map/settlements_1948.yaml name), such as a GDD city.</summary>
        public static string Settlement(Content c, string id) => c.SettlementNames.TryGetValue(id, out var name) ? name : throw Unknown("settlement", id);

        /// <summary>The display name of a designated site (sites.yaml name), never its id.</summary>
        public static string Site(Content c, string id) => c.Sites.FirstOrDefault(x => x.Id == id)?.Name ?? throw Unknown("site", id);

        /// <summary>The display name of what a cause changes: an indicator or policy (model.yaml names), or money
        /// (model.yaml money).</summary>
        public static string Target(Content c, string id) =>
            c.Indicators.FirstOrDefault(i => i.Id == id)?.Name ?? c.Policies.FirstOrDefault(p => p.Id == id)?.Name
            ?? c.Money.FirstOrDefault(m => m.Id == id)?.Name ?? throw Unknown("indicator, policy or money", id);

        /// <summary>The phrase a map action reads as in a sentence ("a transit camp"; rules.yaml map_actions name). Read from the base
        /// rules on purpose: an era override cannot remove a map action, so the base names cover every action an era can have.</summary>
        public static string MapAction(Content c, string id) =>
            c.Rules.MapActions.TryGetValue(id, out var rule) ? rule.Name : throw Unknown("map action", id);

        /// <summary>The phrase with a capital first letter, to start a sentence ("A transit camp").</summary>
        public static string Capitalised(string phrase) =>
            phrase.Length == 0 ? phrase : char.ToUpperInvariant(phrase[0]) + phrase.Substring(1);

        public static bool IsMoney(Content c, string id) => c.Money.Any(m => m.Id == id);

        static MoneyAccount Account(Content c, string id) => c.Money.FirstOrDefault(m => m.Id == id) ?? throw Unknown("money", id);

        static double Finite(double v, string name) =>
            double.IsNaN(v) || double.IsInfinity(v) ? throw new ArgumentOutOfRangeException(name, v, "not a finite number (a programmer error)") : v;

        static string Fixed(double abs, int decimals) => abs.ToString(decimals == 0 ? "0" : "0.0", CultureInfo.InvariantCulture);

        /// <summary>The one sign style: the sign goes after the currency prefix, next to the number ("IL -3.0m";
        /// player feedback, 19 October 2026, #1107), and a value that rounds to zero prints unsigned.
        /// <paramref name="signed"/> adds "+" to positive values (changes).</summary>
        static string Signed(double rounded, string prefix, string body, bool signed) =>
            rounded == 0 ? prefix + body : prefix + (rounded < 0 ? "-" : signed ? "+" : "") + body;

        /// <summary>An amount in millions with its currency: "IL 18.6m", "IL -3.0m", "$41.5m"; zero is "IL 0.0m".</summary>
        public static string Money(Content c, string id, double millions)
        {
            var (sign, prefix, amount) = MoneyParts(c, id, millions);
            return prefix + sign + amount + "m";
        }

        /// <summary>The parts Money is built from, rounded as it rounds: the sign ("-", or "" for a value that rounds to zero
        /// or above), the currency prefix from content, and the amount ("18.6"). The top bar says them in words.</summary>
        public static (string Sign, string Prefix, string Amount) MoneyParts(Content c, string id, double millions)
        {
            var r = Math.Round(Finite(millions, nameof(millions)), 1, MidpointRounding.AwayFromZero);
            return (r < 0 ? "-" : "", Account(c, id).Prefix, Fixed(Math.Abs(r), 1));
        }

        /// <summary>A change in millions with its currency, sign next to the number: "IL +1.2m", "$-0.4m"; zero is "IL 0.0m".</summary>
        public static string MoneyChange(Content c, string id, double millions)
        {
            var r = Math.Round(Finite(millions, nameof(millions)), 1, MidpointRounding.AwayFromZero);
            return Signed(r, Account(c, id).Prefix, Fixed(Math.Abs(r), 1) + "m", signed: true);
        }

        /// <summary>An indicator level (0..1) or a share on the scale the player sees: 0..100.</summary>
        public static double Scale(double level) => Finite(level, nameof(level)) * 100;

        /// <summary>The value the player sees for a level, in points: 0.626 shows as 63.</summary>
        public static double ShownPoints(double level) => Math.Round(Scale(level), 0, MidpointRounding.AwayFromZero);

        /// <summary>The change between two shown levels: the difference of the two shown (rounded) values, so it always
        /// agrees with them (0.626 -> 0.634 shows "63%" -> "63%" and no change; 0.624 -> 0.626 shows "+1%").</summary>
        public static string PercentChangeShown(double before, double now) => PercentChange((ShownPoints(now) - ShownPoints(before)) / 100, 0);

        /// <summary>"55%" for a level of 0.55; zero is "0%".</summary>
        public static string Percent(double level)
        {
            var r = ShownPoints(level);                               // the same rounding as PercentChangeShown
            return Signed(r, "", Fixed(Math.Abs(r), 0) + "%", signed: false);
        }

        /// <summary>"+4.1%" for a change of 0.041 (points on the 0-100 scale, <paramref name="decimals"/> 0 or 1); zero is
        /// "0.0%" (or "0%").</summary>
        public static string PercentChange(double change, int decimals = 1)
        {
            if (decimals != 0 && decimals != 1) throw new ArgumentOutOfRangeException(nameof(decimals), decimals, "0 or 1");
            var r = Math.Round(Scale(change), decimals, MidpointRounding.AwayFromZero);
            return Signed(r, "", Fixed(Math.Abs(r), decimals) + "%", signed: true);
        }

        /// <summary>"+5 units" for a change to a whole-unit national stock (the Food and Arms pools; R-EFX-001).</summary>
        public static string UnitsChange(double change)
        {
            var r = Math.Round(Finite(change, nameof(change)), 0, MidpointRounding.AwayFromZero);
            return Signed(r, "", Fixed(Math.Abs(r), 0) + " units", signed: true);
        }

        /// <summary>"Morale +4.1%" or "Treasury IL -2.0m": the name and the change, in the target's unit.</summary>
        public static string Change(Content c, string id, double change) =>
            (id == Pools.FoodTarget ? "Food" : id == Pools.ArmsTarget ? "Arms" : Target(c, id)) + " "
            + (id == Pools.FoodTarget || id == Pools.ArmsTarget ? UnitsChange(change) : IsMoney(c, id) ? MoneyChange(c, id, change) : PercentChange(change));
    }
}
