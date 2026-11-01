#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game;

/// <summary>The store-neutral seam (#2152): a no-op by default, so web, itch.io and plain desktop builds never talk to a
/// store; a Steam build (#2154) sets its own services.</summary>
public class PlatformServicesTests
{
    [TearDown] public void Reset() => PlatformServices.Current = null!;

    [Test] public void The_default_is_the_no_op_services()
    {
        Assert.AreSame(NullPlatformServices.Instance, PlatformServices.Current);
        Assert.AreEqual("none", PlatformServices.Current.Name);
    }

    [Test] public void Setting_null_restores_the_default()
    {
        PlatformServices.Current = new FakePlatform();
        PlatformServices.Current = null!;
        Assert.AreSame(NullPlatformServices.Instance, PlatformServices.Current);
    }

    [Test] public void The_no_op_unlock_accepts_anything()
    {
        Assert.DoesNotThrow(() => NullPlatformServices.Instance.Unlock(new[] { "OBJ_RAILWAY" }));
    }
}

/// <summary>A store that records every Unlock call (AchievementRulesTests uses it too).</summary>
internal sealed class FakePlatform : IPlatformServices
{
    public readonly List<string[]> Unlocks = new List<string[]>();
    public string Name => "fake";
    public void Unlock(IReadOnlyCollection<string> achievementIds) => Unlocks.Add(achievementIds.ToArray());
}
