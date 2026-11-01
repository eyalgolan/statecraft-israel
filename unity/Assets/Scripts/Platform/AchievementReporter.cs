#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.UI;

namespace Statecraft.Game
{
    /// <summary>Reports the achievements the game has earned (<see cref="AchievementRules"/>) to the store: once when made
    /// (a loaded save may already have some) and after each <see cref="SimBridge.Changed"/>, passing only ids this reporter
    /// has not reported. GameRoot makes a new reporter at each Begin (a load, a new game, a language switch), which offers
    /// what was earned again, so a store's Unlock must be idempotent (<see cref="IPlatformServices.Unlock"/>). It reads the
    /// turn's start (the saved state), never the order preview. With the no-op store it reads nothing. Nothing it does can
    /// throw out of it: a failure (the store's or the rules') is logged once, until a report succeeds again, and what was not
    /// taken is offered again on the next change.</summary>
    public sealed class AchievementReporter : IDisposable
    {
        readonly SimBridge bridge;
        readonly Func<IPlatformServices> services;
        readonly HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
        bool failing;

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
            try
            {
                var store = services();
                if (store is NullPlatformServices) return;   // nothing takes them: offer them to a store set later
                var fresh = AchievementRules.Earned(bridge.TurnStart).Where(id => !reported.Contains(id)).ToList();
                if (fresh.Count == 0) return;
                store.Unlock(fresh);
                reported.UnionWith(fresh);
                failing = false;
            }
            catch (Exception e)
            {
                if (!failing) UnityEngine.Debug.LogException(e);   // a store that stays down does not fill the log
                failing = true;
            }
        }

        public void Dispose() => bridge.Changed -= Report;
    }
}
