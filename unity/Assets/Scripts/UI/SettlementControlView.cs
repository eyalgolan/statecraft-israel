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
            return record.State switch
            {
                SettlementControl.AwaitingChoice => "awaiting_choice",
                SettlementControl.Occupied => "occupied",
                SettlementControl.AwaitingEnd => "awaiting_end",
                _ => null,
            };
        }

        public static string? MapText(UiText text, SettlementControl? record) =>
            Key(record) is string key ? text["map.control." + key] : null;

        public static string? PanelText(UiText text, string place, SettlementControl? record) =>
            Key(record) is string key ? text.Format("info.control." + key, place, record!.Turns) : null;
    }
}
