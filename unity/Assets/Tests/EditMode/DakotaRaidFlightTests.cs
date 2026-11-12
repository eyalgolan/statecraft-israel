#nullable enable
using NUnit.Framework;
using Statecraft.Game.Map.Collection;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>New game's raid flight (#3000, PR 2 of #2968): the Dakotas fly in from the south-west along the coast, over Tel Aviv
/// on their loop's own pass (where their bombs fall, SeaAirCollectionTests), and away south (DakotaRaidFlight). Pure path
/// checks; the scene's aircraft and bombs are in SeaAirCollectionTests, the flight's place in a new game in
/// RaidFlightRoutingTests.</summary>
public class DakotaRaidFlightTests
{
    const float Step = 0.01f;

    [TearDown]
    public void Land()
    {
        DakotaRaidFlight.Stop();
        DakotaRaidFlight.EndRepeat();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    static Vector2 At(float seconds, int plane) => DakotaRaidFlight.Offset(DakotaRaidFlight.PathTime(DakotaRaidFlight.U(seconds, plane)));

    static Vector3 Forward(float seconds, int plane, out float bank)
    {
        DakotaRaidFlight.Turn(DakotaRaidFlight.PathTime(DakotaRaidFlight.U(seconds, plane)), out var forward, out bank);
        return forward;
    }

    [Test]
    public void ThePassOverTelAvivIsTheLoopItself()
    {
        for (float u = -DakotaRaidFlight.PassS; u <= DakotaRaidFlight.PassS; u += 0.25f)
        {
            Assert.AreEqual(u, DakotaRaidFlight.PathTime(u), 1e-5f, "real pace on the pass");
            var turn = DakotaRaidFlight.Turn(u, out var forward, out float bank);
            var loop = SeaAirCollection.DakotaTurn(u, out var loopForward, out float loopBank);
            Assert.Less((DakotaRaidFlight.Offset(u) - SeaAirCollection.DakotaLoopOffset(u)).magnitude, 1e-3f, $"on the loop at {u}");
            Assert.Less(Quaternion.Angle(turn, loop), 1e-3f, $"turned as on the loop at {u}");
            Assert.AreEqual(loopBank, bank, 1e-4f);
            Assert.Less((forward - loopForward).magnitude, 1e-5f);
        }
        Assert.Less((At(DakotaRaidFlight.ArriveS, 0) - SeaAirCollection.DakotaLoopOffset(0f)).magnitude, 1e-3f, "the lead over Tel Aviv at ArriveS");
        Assert.Less((At(DakotaRaidFlight.ArriveS + DakotaRaidFlight.TrailS, 1) - SeaAirCollection.DakotaLoopOffset(0f)).magnitude, 1e-3f, "the wing TrailS later");
        Assert.AreEqual(3.5f, DakotaRaidFlight.U(DakotaRaidFlight.SecondsAt(3.5f, 1), 1), 1e-4f, "SecondsAt is U's inverse");
    }

    [Test]
    public void ItComesFromTheSouthWestAlongTheCoastAndLeavesSouth()
    {
        var telAviv = SeaAirCollection.DakotaLoopOffset(0f);
        var from = At(0f, 0) - telAviv;
        var to = At(DakotaRaidFlight.Seconds, 1) - telAviv;
        Assert.Less((from - DakotaRaidFlight.FromM).magnitude, 1f, $"the lead starts at FromM ({from})");
        Assert.Less((to - DakotaRaidFlight.ToM).magnitude, 1f, $"the wing ends at ToM ({to})");
        Assert.Less(from.x, 0f, "west of Tel Aviv"); Assert.Less(from.y, 0f, "south of it");
        Assert.Less(to.y, -20000f, "far south of it at the end");
        var inbound = Forward(0f, 0, out _);
        Assert.Greater(inbound.z, 0.9f, "flying north along the coast");
        Assert.Greater(inbound.x, 0f, "a little east of north");
        var outbound = Forward(DakotaRaidFlight.Seconds, 1, out _);
        Assert.Less(outbound.z, -0.99f, "flying south at the end");
        // Every point is south of the pass's latitude plus the loop's southern half-width: it never crosses the loop's north.
        for (float s = 0f; s <= DakotaRaidFlight.Seconds; s += 0.5f)
            for (int p = 0; p < 2; p++)
                Assert.Less(At(s, p).y, 0f, $"south of the loop's centre at {s} s (plane {p})");
    }

    [Test]
    public void ThePathIsSmoothAndBanksLikeTheLoop()
    {
        float maxStep = 0f, maxHeading = 0f, maxBank = 0f, most = 0f;
        for (int p = 0; p < 2; p++)
        {
            var last = At(0f, p); var lastForward = Forward(0f, p, out float lastBank);
            for (float s = Step; s <= DakotaRaidFlight.Seconds + 1e-4f; s += Step)
            {
                var at = At(s, p); var forward = Forward(s, p, out float bank);
                maxStep = Mathf.Max(maxStep, (at - last).magnitude);
                maxHeading = Mathf.Max(maxHeading, Vector3.Angle(forward, lastForward));
                maxBank = Mathf.Max(maxBank, Mathf.Abs(bank - lastBank));
                most = Mathf.Max(most, Mathf.Abs(bank));
                Assert.AreEqual(0f, forward.y, 1e-6f); Assert.AreEqual(1f, forward.magnitude, 1e-4f);
                last = at; lastForward = forward; lastBank = bank;
            }
        }
        TestContext.WriteLine($"per {Step} s: step {maxStep:0.0} m, heading {maxHeading:0.00} deg, bank {maxBank:0.00} deg; most bank {most:0.0} deg");
        Assert.Less(maxStep, 100f, "no jump");
        Assert.Less(maxHeading, 1f, "no snap in heading");
        Assert.Less(maxBank, 1f, "no snap in bank");
        Assert.LessOrEqual(most, Statecraft.Game.Map.Units.UnitShowcase.BankDegrees + 1e-3f, "never past the tokens' bank");
    }

    [Test]
    public void BothTurnsAreToTheRightAndBankRight()
    {
        // In, from 20 degrees to the pass's east, and out, from east to south: both to the right, so their bank is negative,
        // as the loop's left turns bank positive. Level (within the loop's own bank at the join) everywhere else.
        float join = Mathf.Max(SeaAirCollection.DakotaBankDegrees(-DakotaRaidFlight.PassS), SeaAirCollection.DakotaBankDegrees(DakotaRaidFlight.PassS));
        foreach (var (from, to, name) in new[] {
            (0f, DakotaRaidFlight.SecondsAt(-DakotaRaidFlight.PassS, 0), "in"),
            (DakotaRaidFlight.SecondsAt(DakotaRaidFlight.PassS, 0), DakotaRaidFlight.Seconds - DakotaRaidFlight.TrailS, "out") })
        {
            float least = 0f, most = float.MinValue;
            for (float s = from; s <= to; s += Step) { Forward(s, 0, out float bank); least = Mathf.Min(least, bank); most = Mathf.Max(most, bank); }
            TestContext.WriteLine($"turn {name}: bank {least:0.0} to {most:0.00} deg");
            Assert.Less(least, -10f, $"banked right on the way {name}");
            Assert.LessOrEqual(most, join + 1e-3f, $"never banked left on the way {name}, past the loop's own bank where it joins");
        }
    }

    [Test]
    public void TheWingNeverMeetsTheLead()
    {
        float apart = 2f * SeaAirCollection.DakotaReachShare * SeaAirCollection.SizeOf(SeaAirCollection.Dakota), least = float.MaxValue;
        for (float s = 0f; s <= DakotaRaidFlight.Seconds; s += Step)
            least = Mathf.Min(least, (At(s, 0) - At(s, 1)).magnitude);
        TestContext.WriteLine($"closest {least:0} m, {apart:0} m needed");
        Assert.Greater(least, apart, "every point of a Dakota is within DakotaReachShare of its span of its centre");
    }

    [Test]
    public void ItLastsUntilTheWingHasLeft()
    {
        Assert.AreEqual(DakotaRaidFlight.PassS + DakotaRaidFlight.TurnS, DakotaRaidFlight.PathTime(DakotaRaidFlight.U(DakotaRaidFlight.Seconds, 1)), 1e-3f,
            "the wing at the end of its way out");
        Assert.AreEqual(-(DakotaRaidFlight.PassS + DakotaRaidFlight.TurnS), DakotaRaidFlight.PathTime(DakotaRaidFlight.U(0f, 0)), 1e-3f,
            "the lead at the start of its way in");
        Assert.That(DakotaRaidFlight.Seconds, Is.InRange(30f, 50f), "not a minute long");
    }

    [Test]
    public void ItFliesOnlyWithADakotaAndEndsByTheMapClock()
    {
        Assert.IsFalse(DakotaRaidFlight.Start(), "no Dakota in an empty scene: nothing flies");
        Assert.IsFalse(DakotaRaidFlight.Flying);
        Assert.IsTrue(DakotaRaidFlight.Over, "not flying is over");
        var clock = SpitfireLoop.Clock;
        var go = new GameObject("dakota", typeof(DakotaLoop));
        try
        {
            float now = 100f;
            SpitfireLoop.Clock = () => now;
            Assert.IsTrue(DakotaRaidFlight.Start());
            Assert.IsTrue(DakotaRaidFlight.Flying);
            now += 1f;
            Assert.IsTrue(DakotaRaidFlight.Since(out float seconds)); Assert.AreEqual(1f, seconds, 1e-4f);
            Assert.IsFalse(DakotaRaidFlight.Over);
            now = 100f + DakotaRaidFlight.Seconds;
            Assert.IsTrue(DakotaRaidFlight.Over, "over after Seconds");
            Assert.IsTrue(DakotaRaidFlight.Flying, "until it is stopped");
            DakotaRaidFlight.Stop();
            Assert.IsFalse(DakotaRaidFlight.Flying);
            DakotaRaidFlight.Stop();   // safe twice
        }
        finally { SpitfireLoop.Clock = clock; Object.DestroyImmediate(go); }
    }

    /// <summary>#3140 (owner, 11 November 2026, "Repeat the raid (Recommended)"): in the first bombing period the raid repeats,
    /// each raid <see cref="DakotaRaidFlight.Seconds"/> long and then a pause of <see cref="DakotaRaidFlight.PauseS"/> with
    /// both aircraft off their path, then again. Each aircraft is on its path from its start to its end only: the lead from 0
    /// to Seconds - TrailS, the wing TrailS later; in the pause neither is.</summary>
    [Test]
    public void EachRaidIsTheFlightAndThenAPauseWithNeitherAircraftOnItsPath()
    {
        Assert.That(DakotaRaidFlight.PauseS, Is.InRange(5f, 30f), "a pause, not a gap of minutes");
        Assert.AreEqual(DakotaRaidFlight.Seconds + DakotaRaidFlight.PauseS, DakotaRaidFlight.CycleS, 1e-4f);
        float lead = DakotaRaidFlight.Seconds - DakotaRaidFlight.TrailS;
        foreach (var (s, plane, on) in new[] {
            (0f, 0, true), (DakotaRaidFlight.ArriveS, 0, true), (lead - 0.01f, 0, true), (lead + 0.01f, 0, false),
            (DakotaRaidFlight.TrailS - 0.01f, 1, false), (DakotaRaidFlight.TrailS + 0.01f, 1, true), (DakotaRaidFlight.Seconds - 0.01f, 1, true),
            (DakotaRaidFlight.Seconds + 0.01f, 1, false), (DakotaRaidFlight.CycleS - 0.01f, 0, false), (DakotaRaidFlight.CycleS - 0.01f, 1, false) })
            Assert.AreEqual(on, DakotaRaidFlight.OnPath(s, plane), $"plane {plane} on its path {s:0.00} s into a raid");
    }

    /// <summary>#3140: the repeat runs by the map's clock from when it started (the Dakotas drawn on a first-period turn, or New
    /// game's raid, which is its first): <see cref="DakotaRaidFlight.Cycle"/> is the seconds into the raid now and the raid's
    /// number. New game's raid starts it over; stopping that raid leaves it running, so the aircraft fly on where they are.</summary>
    [Test]
    public void TheRepeatRunsByTheMapClockAndOutlivesNewGamesRaid()
    {
        var clock = SpitfireLoop.Clock;
        var go = new GameObject("dakota", typeof(DakotaLoop));
        try
        {
            float now = 50f;
            SpitfireLoop.Clock = () => now;
            Assert.IsFalse(DakotaRaidFlight.Repeating);
            DakotaRaidFlight.KeepRepeating();
            Assert.IsTrue(DakotaRaidFlight.Repeating);
            now += 3f;
            Assert.AreEqual(3f, DakotaRaidFlight.Cycle(out int raid), 1e-4f); Assert.AreEqual(0, raid);
            now = 50f + 2f * DakotaRaidFlight.CycleS + 4f;
            Assert.AreEqual(4f, DakotaRaidFlight.Cycle(out raid), 1e-3f); Assert.AreEqual(2, raid, "the third raid");
            DakotaRaidFlight.KeepRepeating();
            Assert.AreEqual(4f, DakotaRaidFlight.Cycle(out _), 1e-3f, "already repeating: it runs on");

            now = 1000f;
            Assert.IsTrue(DakotaRaidFlight.Start());
            now += 7f;
            Assert.AreEqual(7f, DakotaRaidFlight.Cycle(out raid), 1e-4f, "New game's raid is the repeat's first"); Assert.AreEqual(0, raid);
            DakotaRaidFlight.Stop();
            Assert.IsTrue(DakotaRaidFlight.Repeating, "stopping the raid leaves the repeat running");
            Assert.AreEqual(7f, DakotaRaidFlight.Cycle(out _), 1e-4f);
            DakotaRaidFlight.EndRepeat();
            Assert.IsFalse(DakotaRaidFlight.Repeating);
            DakotaRaidFlight.EndRepeat();   // safe twice
        }
        finally { SpitfireLoop.Clock = clock; Object.DestroyImmediate(go); }
    }

    /// <summary>#3140: a raid's bombs are its pass's only. An aircraft's raid time is its loop time (DakotaLoop's bombs read
    /// it), and the loop drops on each pass over Tel Aviv, every DakotaLoopSeconds; so from the wing's start to the lead's end
    /// of a whole cycle, pause included, neither aircraft reaches the loop's next pass, PassS short of a loop either way.</summary>
    [Test]
    public void ARaidAndItsPauseNeverReachTheLoopsNextPass()
    {
        float reach = SeaAirCollection.DakotaLoopSeconds - DakotaRaidFlight.PassS;
        Assert.Greater(DakotaRaidFlight.U(0f, 1), -reach, "the wing at the cycle's start");
        Assert.Less(DakotaRaidFlight.U(DakotaRaidFlight.CycleS, 0), reach, "the lead at the cycle's end");
    }
}
