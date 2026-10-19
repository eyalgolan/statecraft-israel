#nullable enable
using System;
using NUnit.Framework;
using Statecraft.HexTiles;

/// <summary>The hex-tile switch is opt-in and strict, as MapLook's is (the default game must not change).</summary>
public class HexTilesLookTests
{
    [TestCase("http://localhost/?hex-tiles", true)]
    [TestCase("http://localhost/?hex-tiles=1", true)]
    [TestCase("http://localhost/?map-look&hex-tiles", true)]
    [TestCase("http://localhost/?hex-tile", false)]
    [TestCase("http://localhost/?hex-tiles=0", false)]
    [TestCase("http://localhost/#hex-tiles", false)]
    [TestCase("http://localhost/?next=/p?hex-tiles", false)]
    [TestCase(null, false)]
    public void TheWebFlagIsStrict(string? url, bool expected) =>
        Assert.AreEqual(expected, HexTilesLook.Requested(url, Array.Empty<string>(), webgl: true));

    [Test]
    public void TheDesktopFlagIsTheExactArgument()
    {
        Assert.IsTrue(HexTilesLook.Requested(null, new[] { "Statecraft", "-hex-tiles" }, webgl: false));
        Assert.IsFalse(HexTilesLook.Requested(null, new[] { "-hex-tiles-x" }, webgl: false));
        Assert.IsFalse(HexTilesLook.Requested(null, Array.Empty<string>(), webgl: false));
    }

    [Test]
    public void TheBoardShipsInResources() => Assert.That(HexBoard.Load(HexTilesLook.Board).Hexes.Count, Is.GreaterThan(0));
}
