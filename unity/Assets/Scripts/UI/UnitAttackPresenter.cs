#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>Owns only live attack playback; no ghosts, deferred sources, gameplay changes or shot queue.</summary>
    public sealed class UnitAttackPresenter : IDisposable
    {
        readonly SimBridge bridge;
        readonly Func<int, bool> play;
        public UnitAttackPresenter(SimBridge bridge, Func<int, bool> play)
        {
            this.bridge = bridge;
            this.play = play;
            bridge.CombatResolved += Resolved;
        }
        void Resolved(IReadOnlyList<Cause> causes)
        {
            foreach (var cause in causes)
                foreach (int id in AttackAnimationTargets.From(cause))
                    play(id); // Missing/destroyed/incompatible tokens return false; never retain their event.
        }
        public void Dispose() => bridge.CombatResolved -= Resolved;
    }
}
