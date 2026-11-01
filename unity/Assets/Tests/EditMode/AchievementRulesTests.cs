#nullable enable
using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The five achievements (#2152; owner Q5 "Five (Recommended)"): one per objective in content/objectives.yaml and
/// one for finishing the campaign, read from the game state only; the reporter unlocks each once.</summary>
public class AchievementRulesTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);

    static GameState NewState() => GameSession.New(C, GoldenRun.Seed, Mode.Historical).State;

    [TearDown] public void Reset() => PlatformServices.Current = null!;

    [Test] public void Every_objective_in_content_has_an_achievement()
    {
        CollectionAssert.AreEquivalent(C.Objectives.Select(o => o.Id), AchievementRules.ForObjective.Keys);
    }

    [Test] public void The_api_names_are_the_ones_the_owner_enters_in_steamworks()
    {
        CollectionAssert.AreEquivalent(new[] { "OBJ_RAILWAY", "OBJ_BURMA_ROAD", "OBJ_YISHUVIM", "OBJ_ABSORB_IMMIGRANTS" },
            AchievementRules.ForObjective.Values);
        Assert.AreEqual("OBJ_RAILWAY", AchievementRules.ForObjective["railway"]);
        Assert.AreEqual("OBJ_BURMA_ROAD", AchievementRules.ForObjective["burma_road"]);
        Assert.AreEqual("OBJ_YISHUVIM", AchievementRules.ForObjective["yishuvim"]);
        Assert.AreEqual("OBJ_ABSORB_IMMIGRANTS", AchievementRules.ForObjective["absorb_immigrants"]);
        Assert.AreEqual("CAMPAIGN_DONE", AchievementRules.CampaignDone);
    }

    [Test] public void A_new_game_has_earned_nothing()
    {
        CollectionAssert.IsEmpty(AchievementRules.Earned(NewState()));
    }

    [Test] public void A_completed_objective_earns_its_achievement()
    {
        var s = NewState();
        s.Objectives["railway"] = new DateTime(1949, 3, 15);
        CollectionAssert.AreEqual(new[] { "OBJ_RAILWAY" }, AchievementRules.Earned(s));
    }

    [Test] public void Earned_is_sorted_ordinally()
    {
        var s = NewState();
        s.Objectives["yishuvim"] = new DateTime(1949, 3, 15);
        s.Objectives["absorb_immigrants"] = new DateTime(1949, 4, 15);
        s.GameOver = GameOverCodes.MvpEnded;
        CollectionAssert.AreEqual(new[] { "CAMPAIGN_DONE", "OBJ_ABSORB_IMMIGRANTS", "OBJ_YISHUVIM" }, AchievementRules.Earned(s));
    }

    [Test] public void Reaching_the_last_turn_earns_the_campaign_but_a_defeat_does_not()
    {
        var s = NewState();
        s.GameOver = GameOverCodes.MvpEnded;
        CollectionAssert.Contains(AchievementRules.Earned(s), AchievementRules.CampaignDone);
        foreach (var other in new[] { GameOverCodes.DefeatWar, GameOverCodes.DefeatEconomy, GameOverCodes.CoalitionCollapse, GameOverCodes.EraEnded })
        {
            s.GameOver = other;
            CollectionAssert.DoesNotContain(AchievementRules.Earned(s), AchievementRules.CampaignDone, other);
        }
    }

    /// <summary>A finished game with the railway done, as a loaded save holds it.</summary>
    static GameSession Finished(params string[] done)
    {
        var session = GameSession.New(C, GoldenRun.Seed, Mode.Historical);
        foreach (var id in done) session.State.Objectives[id] = new DateTime(1949, 3, 15);
        session.State.GameOver = GameOverCodes.MvpEnded;
        return session;
    }

    [Test] public void The_reporter_unlocks_each_achievement_once_even_over_a_loaded_finished_game()
    {
        var fake = new FakePlatform();
        var bridge = new SimBridge(Finished("railway"), En);
        using (new AchievementReporter(bridge, () => fake))
        {
            bridge.Replace(Finished("railway"));   // Changed: the same finished game loaded again
            bridge.Replace(Finished("railway"));   // Changed again
        }
        Assert.AreEqual(1, fake.Unlocks.Count);
        CollectionAssert.AreEqual(new[] { "CAMPAIGN_DONE", "OBJ_RAILWAY" }, fake.Unlocks[0]);
    }

    [Test] public void The_reporter_passes_only_what_is_new_and_stops_when_disposed()
    {
        var fake = new FakePlatform();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), En);
        var reporter = new AchievementReporter(bridge, () => fake);
        Assert.AreEqual(0, fake.Unlocks.Count, "a new game reports nothing");
        bridge.Replace(Finished("railway"));
        bridge.Replace(Finished("railway", "burma_road"));
        reporter.Dispose();
        bridge.Replace(Finished("railway", "burma_road", "yishuvim"));
        Assert.AreEqual(2, fake.Unlocks.Count);
        CollectionAssert.AreEqual(new[] { "CAMPAIGN_DONE", "OBJ_RAILWAY" }, fake.Unlocks[0]);
        CollectionAssert.AreEqual(new[] { "OBJ_BURMA_ROAD" }, fake.Unlocks[1]);
    }

    /// <summary>A store that fails (Steam not running, say) never stops the game starting, and what it failed to take is
    /// offered again on the next change.</summary>
    [Test] public void A_failing_store_is_logged_and_offered_the_same_achievements_again()
    {
        var fake = new FakePlatform();
        bool fail = true;
        IPlatformServices Services() => fail ? new ThrowingPlatform() : fake;
        var bridge = new SimBridge(Finished("railway"), En);
        LogAssert.Expect(LogType.Exception, new Regex("store offline"));
        using (new AchievementReporter(bridge, Services))
        {
            fail = false;
            bridge.Replace(Finished("railway"));
        }
        Assert.AreEqual(1, fake.Unlocks.Count);
        CollectionAssert.AreEqual(new[] { "CAMPAIGN_DONE", "OBJ_RAILWAY" }, fake.Unlocks[0]);
    }

    sealed class ThrowingPlatform : IPlatformServices
    {
        public string Name => "throwing";
        public void Unlock(System.Collections.Generic.IReadOnlyCollection<string> achievementIds) => throw new InvalidOperationException("store offline");
    }

    /// <summary>A store set after the game started (a late Steam start) still gets what was earned before it: the no-op
    /// store takes nothing, so nothing counts as reported to it.</summary>
    [Test] public void The_reporter_asks_for_the_current_services_each_time()
    {
        var fake = new FakePlatform();
        var bridge = new SimBridge(Finished("railway"), En);
        using (new AchievementReporter(bridge, () => PlatformServices.Current))
        {
            Assert.AreEqual(0, fake.Unlocks.Count);
            PlatformServices.Current = fake;
            bridge.Replace(Finished("railway", "burma_road"));
        }
        Assert.AreEqual(1, fake.Unlocks.Count);
        CollectionAssert.AreEqual(new[] { "CAMPAIGN_DONE", "OBJ_BURMA_ROAD", "OBJ_RAILWAY" }, fake.Unlocks[0]);
    }
}
