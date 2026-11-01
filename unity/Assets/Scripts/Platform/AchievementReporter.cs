#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.UI;

namespace Statecraft.Game
{
    /// <summary>Reports the achievements the game has earned (<see cref="AchievementRules"/>) to the store: once when made
    /// (a loaded save may already have some) and after each <see cref="SimBridge.Changed"/>, passing only ids not reported
    /// before in this session. It reads the turn's start (the saved state), never the order preview. A store that throws is
    /// logged, and what it failed to take is offered again on the next change. With the default no-op services it does nothing
    /// a player can see.</summary>
    public sealed class AchievementReporter : IDisposable
    {
        readonly SimBridge bridge;
        readonly Func<IPlatformServices> services;
        readonly HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);

        /// <param name="services">Asked on every report, so a store set after the game starts is used and gets everything earned
        /// so far (nothing counts as reported to the no-op store).</param>
        public AchievementReporter(SimBridge bridge, Func<IPlatformServices> services)
        {
            this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            bridge.Changed += Report;
            Report();
        }

        void Report()
        {
            var fresh = AchievementRules.Earned(bridge.TurnStart).Where(id => !reported.Contains(id)).ToList();
            if (fresh.Count == 0) return;
            try
            {
                var store = services();
                if (store is NullPlatformServices) return;   // nothing takes them: offer them to a store set later
                store.Unlock(fresh);
                reported.UnionWith(fresh);
            }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }

        public void Dispose() => bridge.Changed -= Report;
    }
}
