#nullable enable
using System;
using System.IO;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
/// <summary>Spec 15: the help level, the seen marks (one compact line) and the opening chain's mark live in the settings
/// file, never in the save; a write that fails costs nothing but persistence.</summary>
public class AdvisorSettingsTests
{
    string dir = "";
    [SetUp] public void Temp() => dir = Path.Combine(Path.GetTempPath(), "statecraft-advisors-" + Guid.NewGuid().ToString("N"));
    [TearDown] public void Clean() { try { if (Directory.Exists(dir)) Directory.Delete(dir, true); if (File.Exists(dir)) File.Delete(dir); } catch (Exception) { } }

    [Test]
    public void TheLevelAndTheSeenMarksRoundTrip()
    {
        var store = new SettingsStore(dir);
        var a = new AdvisorSettings(store);
        Assert.AreEqual("new", a.Level, "spec 6: New to strategy games by default");
        a.SetLevel("warnings"); a.MarkSeen("b.two"); a.MarkSeen("a.one"); a.MarkSeen("a.one");
        Assert.AreEqual("warnings", store.Read(AdvisorSettings.LevelKey));
        Assert.AreEqual("a.one,b.two", store.Read(AdvisorSettings.SeenKey), "one line, ordinal order, no repeats");
        var b = new AdvisorSettings(store);
        Assert.AreEqual(("warnings", 2), (b.Level, b.LevelRank));
        Assert.IsTrue(b.Seen("a.one") && b.Seen("b.two") && !b.Seen("c"));
    }

    [Test]
    public void AnUnknownLevelReadsAsNew()
    {
        var store = new SettingsStore(dir);
        store.Write(AdvisorSettings.LevelKey, "chatty");
        Assert.AreEqual("new", new AdvisorSettings(store).Level);
        Assert.Throws<ArgumentException>(() => new AdvisorSettings(store).SetLevel("chatty"));
    }

    /// <summary>Spec 9.3: a player who saw the first-turn guide does not get the opening chain by itself.</summary>
    [Test]
    public void TheGuidesSeenLineCountsAsTheOpeningSeen()
    {
        var store = new SettingsStore(dir);
        Assert.IsFalse(new AdvisorSettings(store).OpeningSeen);
        store.Write(AdvisorSettings.GuideSeenKey, "seen");
        Assert.IsTrue(new AdvisorSettings(store).OpeningSeen);
        var fresh = new SettingsStore(dir + "-b");
        var a = new AdvisorSettings(fresh);
        a.MarkOpeningSeen();
        Assert.AreEqual("seen", fresh.Read(AdvisorSettings.OpeningKey));
        Directory.Delete(dir + "-b", true);
    }

    /// <summary>No file (automated runs) and a failed write (a full or blocked IndexedDB) both keep the session's marks in
    /// memory and throw nothing.</summary>
    [Test]
    public void WithoutAFileOrWithAFailedWriteTheMarksHoldForTheSession()
    {
        var none = new AdvisorSettings(null);
        Assert.IsFalse(none.HasFile);
        none.MarkSeen("a"); none.SetLevel("off");
        Assert.IsTrue(none.Seen("a")); Assert.AreEqual("off", none.Level);

        File.WriteAllText(dir, "a file where the folder should be");   // Directory.CreateDirectory throws
        var broken = new AdvisorSettings(new SettingsStore(dir));
        broken.MarkSeen("a"); broken.MarkOpeningSeen();               // logs a warning, throws nothing
        Assert.IsTrue(broken.Seen("a")); Assert.IsTrue(broken.OpeningSeen);
    }
}
