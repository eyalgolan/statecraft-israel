#nullable enable
using System.Collections.Generic;

namespace Statecraft.Game
{
    /// <summary>What the game asks of a store (Steam; #2149). Store-neutral: Statecraft.Game never references a store's SDK,
    /// so web, itch.io and plain desktop builds run the no-op <see cref="NullPlatformServices"/>; a Steam build (#2154) sets
    /// <see cref="PlatformServices.Current"/> to its own.</summary>
    public interface IPlatformServices
    {
        /// <summary>The store's name for logs ("none" for the no-op).</summary>
        string Name { get; }
        /// <summary>Unlocks these achievements by their API names (<see cref="AchievementRules"/>). Must be idempotent: the
        /// reporter offers an id again after GameRoot rebuilds (a load, a new game, a language switch).</summary>
        void Unlock(IReadOnlyCollection<string> achievementIds);
    }
}
