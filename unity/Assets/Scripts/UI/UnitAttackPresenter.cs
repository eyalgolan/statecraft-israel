#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Plays each fresh engagement's attackers' fire take on the map (#1867; design by Tal Heuberger, #2078): the
    /// attacking units' figures, supporting guns included, never the defenders'. Presentation only. A unit whose token cannot
    /// play now (destroyed, hidden, far, or no fire take) is skipped and nothing is kept to play later. GameRoot owns it.</summary>
    public sealed class UnitAttackPresenter : IDisposable
    {
        readonly SimBridge bridge;
        readonly Func<int, bool> play;

        /// <param name="play">Plays a sim unit's attack (UnitLayer.PlayAttack); false when its token cannot.</param>
        public UnitAttackPresenter(SimBridge bridge, Func<int, bool> play)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            this.play = play ?? throw new ArgumentNullException(nameof(play));
            bridge.CombatResolved += Resolved;
        }

        void Resolved(IReadOnlyList<Cause> engagements)
        {
            foreach (var engagement in engagements)
                foreach (int id in Attackers(engagement)) play(id);
        }

        /// <summary>An engagement's attackers, from its structured line (Combat.Engage): the units are the attackers by id,
        /// supporting guns included, then the defenders, and the fifth argument counts the attackers. Never the report's
        /// text. None for another line or a malformed count.</summary>
        internal static IEnumerable<int> Attackers(Cause engagement)
        {
            if (engagement.Key != WhyKeys.Engagement || engagement.Args.Count < 5
                || !int.TryParse(engagement.Args[4], NumberStyles.None, CultureInfo.InvariantCulture, out int count)
                || count < 1 || count > engagement.Units.Count) yield break;
            for (int i = 0; i < count; i++) yield return engagement.Units[i].Id;
        }

        public void Dispose() => bridge.CombatResolved -= Resolved;
    }
}
