#nullable enable
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Pure UI wording for the settlement-control records on main. The sim remains the source of truth.</summary>
    public static class SettlementControlView
    {
        public static string? Key(SettlementControl? record)
        {
            if (record == null) return null;
            if (record.Depopulated) return "depopulated";
            if (record.Owner == "") return "lost";
            return record.State switch
            {
                SettlementControl.AwaitingChoice => "awaiting_choice",
                SettlementControl.Occupied => "occupied",
                SettlementControl.AwaitingEnd => "awaiting_end",
                SettlementControl.Annexed => "annexed",
                _ => null,
            };
        }

        /// <summary>The live holder represented by a settlement-control record: true for Israel, false for the enemy, and
        /// null when there is no record (or a future record whose owner this UI does not understand).</summary>
        public static bool? PlayerHolds(SettlementControl? record) => record?.Owner switch
        {
            Movement.PlayerSide => true,
            "" => false,
            _ => null,
        };

        public static string? MapText(UiText text, SettlementControl? record) =>
            Key(record) is string key ? text["map.control." + key] : null;

        public static string? PanelText(UiText text, string place, SettlementControl? record) =>
            Key(record) is string key ? text.Format("info.control." + key, place, record!.Turns) : null;
    }
}
