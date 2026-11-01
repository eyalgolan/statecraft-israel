#nullable enable
using System.Collections.Generic;

namespace Statecraft.Game
{
    /// <summary>The store the game talks to (#2149). Never null: the default, and what setting null restores, is the no-op
    /// <see cref="NullPlatformServices"/>, which every build but a Steam build keeps.</summary>
    public static class PlatformServices
    {
        static IPlatformServices current = NullPlatformServices.Instance;

        public static IPlatformServices Current
        {
            get => current;
            set => current = value ?? NullPlatformServices.Instance;
        }
    }

    /// <summary>No store: the web, itch.io and plain desktop builds. Unlocks nothing.</summary>
    public sealed class NullPlatformServices : IPlatformServices
    {
        public static readonly NullPlatformServices Instance = new NullPlatformServices();

        NullPlatformServices() { }

        public string Name => "none";

        public void Unlock(IReadOnlyCollection<string> achievementIds) { }
    }
}
