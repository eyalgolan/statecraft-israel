#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

namespace Statecraft.Game
{
    /// <summary>The five achievements (owner Q5 "Five (Recommended)", #2150): one per objective in content/objectives.yaml
    /// and one for finishing the campaign. Read from the game state only; the sim does not know about them. The API names are
    /// the ones the owner enters in Steamworks App Admin; AchievementRulesTests fails when an objective has none.</summary>
    public static class AchievementRules
    {
        /// <summary>The achievement API name for each objective id.</summary>
        public static readonly IReadOnlyDictionary<string, string> ForObjective = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["railway"] = "OBJ_RAILWAY",
            ["burma_road"] = "OBJ_BURMA_ROAD",
            ["yishuvim"] = "OBJ_YISHUVIM",
            ["absorb_immigrants"] = "OBJ_ABSORB_IMMIGRANTS",
        };

        /// <summary>Reaching the GDD game's last day (<see cref="GameOverCodes.MvpEnded"/>); a defeat never earns it.</summary>
        public const string CampaignDone = "CAMPAIGN_DONE";

        /// <summary>The API names <paramref name="s"/> has earned, sorted ordinally.</summary>
        public static IReadOnlyList<string> Earned(GameState s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            var earned = s.Objectives.Keys.Where(ForObjective.ContainsKey).Select(id => ForObjective[id]).ToList();
            if (s.GameOver == GameOverCodes.MvpEnded) earned.Add(CampaignDone);
            earned.Sort(StringComparer.Ordinal);
            return earned;
        }
    }
}
