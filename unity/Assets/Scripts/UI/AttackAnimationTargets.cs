#nullable enable
using System.Collections.Generic;
using System.Globalization;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Reads structured resolved-combat participants, never orders or localized report text.</summary>
    public static class AttackAnimationTargets
    {
        public static IEnumerable<int> From(Cause cause)
        {
            if (cause.Key != WhyKeys.Engagement || cause.Args.Count < 5
                || !int.TryParse(cause.Args[4], NumberStyles.None, CultureInfo.InvariantCulture, out int count)
                || count <= 0 || count > cause.Units.Count) yield break;
            // Combat records attackers first, including supporting guns; defenders follow them.
            for (int i = 0; i < count; i++) yield return cause.Units[i].Id;
        }
    }
}
