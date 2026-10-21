using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Statecraft.Game;
using Statecraft.Game.Boot;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;

public class GameInputsTests
{
    [Test]
    public void TheManifestListsEveryContentFileThePlayerNeeds()
    {
        var files = ContentManifest.Collect(Paths.Content);
        CollectionAssert.Contains(files, "people.yaml");
        CollectionAssert.Contains(files, "events/armistice_egypt.yaml");
        CollectionAssert.Contains(files, "map/hexes.json");
        CollectionAssert.Contains(files, "map/settlements_1948.yaml");
        CollectionAssert.DoesNotContain(files, "map/precip_bio12.u16");   // tool input only
        CollectionAssert.AreEqual(files.OrderBy(f => f, System.StringComparer.Ordinal).ToList(), files);
        Assert.AreEqual(files, ContentManifest.Parse(ContentManifest.Write(files)));
    }

    [Test]
    public void TheManifestListsEraRuleOverridesAndToleratesNoRulesFolder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "manifest-rules-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(dir, "events"));
            Directory.CreateDirectory(Path.Combine(dir, "map"));
            File.WriteAllText(Path.Combine(dir, "people.yaml"), "people: []\n");
            File.WriteAllText(Path.Combine(dir, "map", "hexes.json"), "{}");
            CollectionAssert.DoesNotContain(ContentManifest.Collect(dir), "rules/later.yaml");   // no rules/ folder: fine
            Directory.CreateDirectory(Path.Combine(dir, "rules"));
            File.WriteAllText(Path.Combine(dir, "rules", "later.yaml"), "ceasefire_weeks: 4\n");
            CollectionAssert.Contains(ContentManifest.Collect(dir), "rules/later.yaml");   // #58: players must get override files
        }
        finally { Directory.Delete(dir, true); }
    }

    [Test]
    public void ABrokenManifestIsAClearError()
    {
        var ex = Assert.Throws<InvalidDataException>(() => ContentManifest.Parse("{\"files\": []}"));
        Assert.AreEqual("manifest.json: no files", ex.Message);
    }

    static string StagedSite()
    {
        var site = Path.Combine(Path.GetTempPath(), "statecraft-site-" + System.Guid.NewGuid().ToString("N"));
        ContentManifest.Copy(Paths.Content, Path.Combine(site, "content"));
        Directory.CreateDirectory(Path.Combine(site, "map"));
        foreach (var f in new[] { "heightmap.json", "heightmap.r16z" }) File.Copy(Path.Combine(Paths.StreamingMap, f), Path.Combine(site, "map", f));
        return site;
    }

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator FetchingOverUrlsLoadsTheSameContentAsTheDisk()
    {
        var site = StagedSite();
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        yield return Await(fetch);
        var fetched = fetch.GetResult();
        var disk = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
        CollectionAssert.AreEqual(disk.Map.HeightmapR16z, fetched.Map.HeightmapR16z);
        Assert.AreEqual(disk.Map.HeightmapJson, fetched.Map.HeightmapJson);
        Assert.AreEqual(disk.Map.SettlementsYaml, fetched.Map.SettlementsYaml);
        Assert.AreEqual(disk.Map.RoutesYaml, fetched.Map.RoutesYaml);
        Assert.AreEqual(disk.Map.HexesJson, fetched.Map.HexesJson);
        Assert.AreEqual(disk.Content.Events.Count, fetched.Content.Events.Count);
        Assert.AreEqual(Simulation.NewGame(disk.Content, 7, Mode.Historical).Hash(), Simulation.NewGame(fetched.Content, 7, Mode.Historical).Hash());
        Directory.Delete(site, true);
    }

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator AMissingBundleNamesTheFile()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", ContentManifest.BundleFileName));
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        yield return Await(fetch);
        var ex = Assert.Throws<IOException>(() => fetch.GetResult());
        StringAssert.StartsWith("bundle.json: ", ex.Message);
        Directory.Delete(site, true);
    }

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator AFailedFetchLeavesNoRequestOpen()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", ContentManifest.BundleFileName));
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        yield return Await(fetch);
        Assert.Throws<IOException>(() => fetch.GetResult());
        Assert.AreEqual(0, StreamingFetch.OpenRequests, "requests still in flight after a failed fetch were not disposed");
        Directory.Delete(site, true);
    }

    /// <summary>Seconds a fetch may go without progress before it counts as hung. Not a limit on the whole fetch: under
    /// heavy load (several Unity batch runs, load average 46-97) fetches that pass when run alone failed a fixed 60 s limit
    /// on the whole fetch (issue #503), while a hang shows as no request finishing, retrying or starting for a long time.</summary>
    const float StallSeconds = 60f;
    /// <summary>A generous cap on the whole wait, ten times the stall limit: a fetch that keeps changing its open requests
    /// but never finishes (a retry loop) still fails instead of hanging the EditMode run.</summary>
    const float CapSeconds = 600f;
    /// <summary>The Unity Test Framework's own limit for each test that waits on a fetch, in milliseconds: the cap plus a
    /// minute. Without it UTF stops a [UnityTest] after its default 180 s, before a slow fetch that is still making
    /// progress reaches the cap, and with a bare timeout message instead of the wait's own.</summary>
    const int FetchTestTimeoutMs = (int)(CapSeconds * 1000) + 60_000;

    /// <summary>Waits for a fetch for as long as it makes progress: it fails when StreamingFetch.OpenRequests (requests
    /// started and not yet released) has not changed for StallSeconds, or once CapSeconds have passed in all. The
    /// manifest's request is released and every file's started, then each file's request is released as the fetch takes
    /// it (a retry releases one and starts another), so the count keeps moving while the fetch works.
    /// A batch-mode editor runs the player loop (which resumes Awaitable.NextFrameAsync) only now and then while idle: a
    /// retry's 0.5 s backoff measured 57 s over 2 frames. Each tick queues a player-loop update, as a player runs one
    /// every frame.</summary>
    static IEnumerator Await(Awaitable<GameInputs>.Awaiter fetch) =>
        WaitWhileProgressing(() => fetch.IsCompleted, () => StreamingFetch.OpenRequests, StallSeconds, CapSeconds, () => Time.realtimeSinceStartup, "fetch");

    /// <summary>Yields until <paramref name="done"/>; throws a TimeoutException once <paramref name="progress"/> has not
    /// changed for <paramref name="stallSeconds"/> by <paramref name="clock"/>, or once <paramref name="capSeconds"/> have
    /// passed in all however it moves.</summary>
    static IEnumerator WaitWhileProgressing(Func<bool> done, Func<int> progress, float stallSeconds, float capSeconds, Func<float> clock, string what)
    {
        var last = progress();
        var start = clock();
        var since = start;
        while (!done())
        {
            var now = progress();
            if (clock() - start >= capSeconds)
                throw new TimeoutException($"{what} did not finish within {capSeconds} seconds although it kept making progress");
            if (now != last) { last = now; since = clock(); }
            else if (clock() - since >= stallSeconds)
                throw new TimeoutException($"{what} made no progress for {stallSeconds} seconds (progress stayed at {last})");
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }
    }

    /// <summary>Runs a wait to its end in one go (the fake operations below need no frames).</summary>
    static void RunToEnd(IEnumerator wait) { while (wait.MoveNext()) { } }

    [Test]
    public void TheFetchWaitFailsWhenProgressStopsButNotWhenItIsSlow()
    {
        // A fake clock: each check of the operation takes one second. The stall limit is 5 s.
        var t = 0f;
        Func<float> clock = () => t;

        // A hang: nothing ever changes. It fails once the 5 s pass, not before.
        var hang = Assert.Throws<TimeoutException>(() => RunToEnd(WaitWhileProgressing(() => { t += 1f; return false; }, () => 7, 5f, 1000f, clock, "hung op")));
        StringAssert.StartsWith("hung op made no progress for 5 seconds", hang.Message);
        Assert.That(t, Is.InRange(5f, 6f));

        // Slow but working: a step every 4 s, done after 120 s in all (twice the old fixed 60 s limit). It passes.
        t = 0f;
        Assert.DoesNotThrow(() => RunToEnd(WaitWhileProgressing(() => { t += 1f; return t > 120f; }, () => (int)(t / 4f), 5f, 1000f, clock, "slow op")));
        Assert.That(t, Is.GreaterThan(120f));

        // Progress that stops part way still fails, the stall measured from the last step (at 40 s).
        t = 0f;
        Assert.Throws<TimeoutException>(() => RunToEnd(WaitWhileProgressing(() => { t += 1f; return false; }, () => (int)(Math.Min(t, 40f) / 4f), 5f, 1000f, clock, "stalled op")));
        Assert.That(t, Is.InRange(45f, 46f));

        // Steady progress that never finishes (a retry loop) still fails, at the overall cap (here 200 s).
        t = 0f;
        var loop = Assert.Throws<TimeoutException>(() => RunToEnd(WaitWhileProgressing(() => { t += 1f; return false; }, () => (int)(t / 4f), 5f, 200f, clock, "looping op")));
        StringAssert.StartsWith("looping op did not finish within 200 seconds", loop.Message);
        Assert.That(t, Is.InRange(200f, 201f));
    }

    /// <summary>Fetches the staged site through a FlakySite with these faults and checks the result is the disk's
    /// content (same hash) with nothing left open; returns the server for its hit counts.</summary>
    static IEnumerator FetchesTheDiskContentDespite(Dictionary<string, (string, int)> faults, Action<FlakySite> check)
    {
        var site = StagedSite();
        try
        {
            using (var server = new FlakySite(site, faults))
            {
                var fetch = GameInputs.FetchAsync(server.BaseUrl).GetAwaiter();
                yield return Await(fetch);
                var fetched = fetch.GetResult();
                var disk = GameInputs.FromDisk(Paths.Content, Paths.StreamingMap);
                Assert.AreEqual(disk.Content.Hash, fetched.Content.Hash);
                Assert.AreEqual(Simulation.NewGame(disk.Content, 7, Mode.Historical).Hash(), Simulation.NewGame(fetched.Content, 7, Mode.Historical).Hash());
                CollectionAssert.AreEqual(disk.Map.HeightmapR16z, fetched.Map.HeightmapR16z);
                Assert.AreEqual(0, StreamingFetch.OpenRequests);
                check(server);
            }
        }
        finally { Directory.Delete(site, true); }
    }

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator DroppedConnectionsAreRetried() =>
        FetchesTheDiskContentDespite(new Dictionary<string, (string, int)> { ["content/bundle.json"] = ("drop", 2) },
            server => Assert.AreEqual(3, server.Hits("content/bundle.json")));

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator AServerErrorIsRetried() =>
        FetchesTheDiskContentDespite(new Dictionary<string, (string, int)> { ["content/bundle.json"] = ("503", 1), ["map/heightmap.r16z"] = ("500", 2) },
            server =>
            {
                Assert.AreEqual(2, server.Hits("content/bundle.json"));
                Assert.AreEqual(3, server.Hits("map/heightmap.r16z"));
            });

    /// <summary>#1054: a rate-limited host (itch.io) answers 429, some with a Retry-After. Twice for the bundle and the
    /// heightmap, then 200: the boot still loads the disk's content.</summary>
    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator TooManyRequestsTwiceThenOkStillBoots() =>
        FetchesTheDiskContentDespite(new Dictionary<string, (string, int)> { ["content/bundle.json"] = ("429|Retry-After: 1", 2), ["map/heightmap.r16z"] = ("429", 2) },
            server =>
            {
                Assert.AreEqual(3, server.Hits("content/bundle.json"));
                Assert.AreEqual(3, server.Hits("map/heightmap.r16z"));
            });

    /// <summary>#1054: a 429 with "Retry-After: 3" is fetched again no sooner than 3 s later (the backoff alone is 0.5 s).</summary>
    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator ARetryAfterIsWaitedOut()
    {
        var started = Time.realtimeSinceStartup;
        yield return FetchesTheDiskContentDespite(new Dictionary<string, (string, int)> { ["content/bundle.json"] = ("429|Retry-After: 3", 1) },
            server => Assert.AreEqual(2, server.Hits("content/bundle.json")));
        Assert.GreaterOrEqual(Time.realtimeSinceStartup - started, 3f, "the second attempt came before the Retry-After");
    }

    /// <summary>A scripted download for StreamingFetch.Done: each attempt answers the next (status, Retry-After) at once;
    /// status 0 is no response at all.</summary>
    sealed class FakeDownload : StreamingFetch.IDownload
    {
        readonly Queue<(long status, string retryAfter)> answers;
        (long status, string retryAfter) now;
        public int Attempts = 1, Releases;

        public FakeDownload(params (long, string)[] answers)
        {
            this.answers = new Queue<(long, string)>(answers);
            now = this.answers.Dequeue();
        }

        public bool IsDone => true;
        public ulong DownloadedBytes => 0;
        public UnityWebRequest.Result Result => now.status == 0 ? UnityWebRequest.Result.ConnectionError
            : now.status == 200 ? UnityWebRequest.Result.Success : UnityWebRequest.Result.ProtocolError;
        public long Status => now.status;
        public string Error => "HTTP " + now.status;
        public string Url => "fake://site/x.yaml";
        public string RetryAfter => now.retryAfter;
        public void Release() => Releases++;
        public void Restart() { Attempts++; now = answers.Dequeue(); }
    }

    /// <summary>Runs StreamingFetch.Done on a fake to its end; the exception it ended with, or null.</summary>
    static IEnumerator RunDone(FakeDownload d, Action<Exception> ended)
    {
        var done = StreamingFetch.Done(d, "x.yaml", StreamingFetch.StallSeconds).GetAwaiter();
        while (!done.IsCompleted)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }
        try { done.GetResult(); ended(null); }
        catch (Exception e) { ended(e); }
    }

    /// <summary>#1054, the shared helper with a fake transport: 429, 429, then 200 succeeds at the third attempt.</summary>
    [UnityTest, Timeout(120_000)]
    public IEnumerator TheRetryHelperGetsThroughTwoTooManyRequests()
    {
        var d = new FakeDownload((429, null), (429, "1"), (200, null));
        Exception error = new Exception("not run");
        yield return RunDone(d, e => error = e);
        Assert.IsNull(error);
        Assert.AreEqual(3, d.Attempts);
        Assert.AreEqual(2, d.Releases, "each failed attempt is released before the next (the caller releases the good one)");
    }

    /// <summary>The helper gives up after MaxAttempts with an IOException naming the file, and never retries a 404.</summary>
    [UnityTest, Timeout(120_000)]
    public IEnumerator TheRetryHelperStopsAfterMaxAttemptsAndOnAClientError()
    {
        var d = new FakeDownload((429, null), (0, null), (503, null), (429, null));
        Exception error = null;
        yield return RunDone(d, e => error = e);
        Assert.IsInstanceOf<IOException>(error);
        Assert.AreEqual("x.yaml: HTTP 429 (fake://site/x.yaml) after 4 attempts", error.Message);
        Assert.AreEqual(StreamingFetch.MaxAttempts, d.Attempts);
        Assert.AreEqual(StreamingFetch.MaxAttempts, d.Releases);

        var notFound = new FakeDownload((404, null), (200, null));
        yield return RunDone(notFound, e => error = e);
        Assert.AreEqual("x.yaml: HTTP 404 (fake://site/x.yaml)", error.Message);
        Assert.AreEqual(1, notFound.Attempts);
    }

    /// <summary>The wait before the next attempt: the backoff, or a longer Retry-After (seconds or an HTTP-date), at most
    /// MaxRetryAfter; a malformed or past one is ignored.</summary>
    [Test]
    public void ARetryAfterLongerThanTheBackoffIsHonouredUpToTheCap()
    {
        var now = new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
        Assert.AreEqual(0.5f, StreamingFetch.Delay(1, null, now));
        Assert.AreEqual(3f, StreamingFetch.Delay(1, "3", now));
        Assert.AreEqual(3f, StreamingFetch.Delay(1, " 3 ", now));
        Assert.AreEqual(2f, StreamingFetch.Delay(3, "1", now));   // the backoff is longer
        Assert.AreEqual(StreamingFetch.MaxRetryAfter, StreamingFetch.Delay(1, "3600", now));
        Assert.AreEqual(5f, StreamingFetch.Delay(1, "Sun, 18 Oct 2026 12:00:05 GMT", now));
        Assert.AreEqual(0.5f, StreamingFetch.Delay(1, "Sun, 18 Oct 2026 11:00:00 GMT", now));   // in the past
        Assert.AreEqual(1f, StreamingFetch.Delay(2, "soon", now));
        Assert.AreEqual(1f, StreamingFetch.Delay(2, "-5", now));
    }

    /// <summary>#1054: the content packed into one file at build time gives back every file's text as the disk path reads
    /// it, so the web boot loads the same content with the same content hash.</summary>
    [Test]
    public void ThePackedBundleRoundTripsToTheSameTextsAndContentHash()
    {
        var files = ContentManifest.Collect(Paths.Content);
        var (listed, texts) = ContentManifest.ParseBundle(ContentManifest.WriteBundle(Paths.Content, files));
        CollectionAssert.AreEqual(files, listed);
        Assert.AreEqual(files.Count, texts.Count);
        foreach (var f in files) Assert.AreEqual(File.ReadAllText(Path.Combine(Paths.Content, f)), texts[f], f);
        var packed = GameInputs.FromTexts(texts, listed, "", new byte[0]);
        var disk = ContentLoader.Load(Paths.Content);
        Assert.AreEqual(disk.Hash, packed.Content.Hash);
        Assert.AreEqual(Simulation.NewGame(disk, 7, Mode.Historical).Hash(), Simulation.NewGame(packed.Content, 7, Mode.Historical).Hash());
    }

    [Test]
    public void ABrokenBundleIsAClearError()
    {
        Assert.AreEqual("bundle.json: no files", Assert.Throws<InvalidDataException>(() => ContentManifest.ParseBundle("{\"files\": []}")).Message);
        Assert.AreEqual("bundle.json: 2 files but 1 texts",
            Assert.Throws<InvalidDataException>(() => ContentManifest.ParseBundle("{\"files\": [\"a.yaml\", \"b.yaml\"], \"texts\": [\"x\"]}")).Message);
        Assert.AreEqual("bundle.json: a.yaml twice",
            Assert.Throws<InvalidDataException>(() => ContentManifest.ParseBundle("{\"files\": [\"a.yaml\", \"a.yaml\"], \"texts\": [\"x\", \"y\"]}")).Message);
    }

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator AConnectionThatKeepsDroppingFailsNamingTheFileWithNothingLeftOpen()
    {
        var site = StagedSite();
        try
        {
            var faults = new Dictionary<string, (string, int)> { ["content/bundle.json"] = ("drop", int.MaxValue) };
            using (var server = new FlakySite(site, faults))
            {
                var fetch = GameInputs.FetchAsync(server.BaseUrl).GetAwaiter();
                yield return Await(fetch);
                var ex = Assert.Throws<IOException>(() => fetch.GetResult());
                StringAssert.StartsWith("bundle.json: ", ex.Message);
                StringAssert.EndsWith($"after {StreamingFetch.MaxAttempts} attempts", ex.Message);
                Assert.AreEqual(StreamingFetch.MaxAttempts, server.Hits("content/bundle.json"));
                Assert.AreEqual(0, StreamingFetch.OpenRequests, "requests still in flight after a failed fetch were not disposed");
            }
        }
        finally { Directory.Delete(site, true); }
    }

    /// <summary>#911: the stall limit is on silence, not on the whole download. A file that takes 3 s to arrive, a part every
    /// 0.3 s, passes a 2 s stall limit at its first attempt (a fixed per-request timeout would cut it off).</summary>
    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator ASlowDownloadThatKeepsReceivingIsNotCutOff()
    {
        var site = StagedSite();
        try
        {
            var faults = new Dictionary<string, (string, int)> { ["content/bundle.json"] = ("trickle", 1) };
            using (var server = new FlakySite(site, faults))
            {
                var started = Time.realtimeSinceStartup;
                var fetch = GameInputs.FetchAsync(server.BaseUrl, "", 2f).GetAwaiter();
                yield return Await(fetch);
                var fetched = fetch.GetResult();
                Assert.AreEqual(GameInputs.FromDisk(Paths.Content, Paths.StreamingMap).Content.Hash, fetched.Content.Hash);
                Assert.AreEqual(1, server.Hits("content/bundle.json"), "the slow download was aborted and retried");
                Assert.Greater(Time.realtimeSinceStartup - started, FlakySite.TrickleChunks * FlakySite.TrickleGapMs / 1000f, "the download was slower than the stall limit");
                Assert.AreEqual(0, StreamingFetch.OpenRequests);
            }
        }
        finally { Directory.Delete(site, true); }
    }

    /// <summary>#911: a server that accepts the request and never answers. Each attempt is aborted after the stall limit
    /// (1 s here, GameInputs.StallSeconds in the player), retried up to MaxAttempts, and the boot then shows its failure.
    /// About 8 s (four 1 s stalls and the 0.5, 1 and 2 s backoffs); the 120 s UTF limit only stops a hang.</summary>
    [UnityTest, Timeout(120_000)]
    public IEnumerator ARequestThatNeverCompletesRetriesThenRaisesTheVisibleBootFailure()
    {
        const float testStallSeconds = 1f;
        var site = StagedSite();
        var bootObject = new GameObject("StalledBoot");
        try
        {
            var faults = new Dictionary<string, (string, int)> { ["content/bundle.json"] = ("stall", int.MaxValue) };
            using (var server = new FlakySite(site, faults))
            {
                var boot = bootObject.AddComponent<GameBoot>();
                string failure = null;
                boot.Failed += reason => failure = reason;
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BOOT FAIL bundle.json: .* after 4 attempts"));
                LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("bundle.json: .* after 4 attempts"));

                var started = Time.realtimeSinceStartup;
                var run = boot.BootAsync(() => GameInputs.FetchAsync(server.BaseUrl, "", testStallSeconds)).GetAwaiter();
                while (!run.IsCompleted)
                {
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                    yield return null;
                }
                run.GetResult();

                StringAssert.StartsWith("bundle.json: ", failure);
                StringAssert.EndsWith($"after {StreamingFetch.MaxAttempts} attempts", failure);
                Assert.AreEqual(StreamingFetch.MaxAttempts, server.Hits("content/bundle.json"));
                Assert.Less(Time.realtimeSinceStartup - started,
                    StreamingFetch.MaxAttempts * testStallSeconds + 10f, "boot failure was not visible within the stated retry time");
                Assert.AreEqual(0, StreamingFetch.OpenRequests);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(bootObject);
            Directory.Delete(site, true);
        }
    }

    /// <summary>Issue #437: StreamingAssets keep fixed names, so a returning browser could pair a new build's code with the
    /// previous build's cached content files. Every URL the player fetches carries the build's id; a file path (desktop,
    /// the editor) or an empty id is left alone.</summary>
    [Test]
    public void TheBuildIdGoesOnHttpUrlsOnly()
    {
        Assert.AreEqual("http://localhost:8000/StreamingAssets/content/people.yaml?b=0123abcd",
            GameInputs.WithBuildId("http://localhost:8000/StreamingAssets/content/people.yaml", "0123abcd"));
        Assert.AreEqual("https://html.itch.zone/html/1/StreamingAssets/video/a.mp4?b=0123abcd",
            GameInputs.WithBuildId("https://html.itch.zone/html/1/StreamingAssets/video/a.mp4", "0123abcd"));
        Assert.AreEqual("http://x/a.yaml?v=1&b=0123abcd", GameInputs.WithBuildId("http://x/a.yaml?v=1", "0123abcd"));
        Assert.AreEqual("http://x/a.yaml", GameInputs.WithBuildId("http://x/a.yaml", ""));
        Assert.AreEqual("file:///tmp/site/content/people.yaml", GameInputs.WithBuildId("file:///tmp/site/content/people.yaml", "0123abcd"));
        Assert.AreEqual("/Applications/Statecraft.app/Contents/Resources/Data/StreamingAssets/video/a.mp4",
            GameInputs.WithBuildId("/Applications/Statecraft.app/Contents/Resources/Data/StreamingAssets/video/a.mp4", "0123abcd"));
    }

    /// <summary>#1054: the web boot makes three requests (the content bundle and the heightmap's two files), not one per
    /// content file (66 StreamingAssets requests on the live v0.1.1 build), and every one carries the build id.</summary>
    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator EveryFetchCarriesTheBuildIdRetriesIncluded()
    {
        var site = StagedSite();
        try
        {
            var faults = new Dictionary<string, (string, int)> { ["content/bundle.json"] = ("503", 1) };
            using (var server = new FlakySite(site, faults))
            {
                var fetch = GameInputs.FetchAsync(server.BaseUrl, "0123abcd").GetAwaiter();
                yield return Await(fetch);
                fetch.GetResult();
                var targets = server.Targets.ToArray();
                CollectionAssert.AreEquivalent(new[] { "/content/bundle.json", "/content/bundle.json", "/map/heightmap.json", "/map/heightmap.r16z" },
                    targets.Select(t => t.Split('?')[0]).ToArray());   // the bundle (once retried) and the heightmap's two files
                foreach (var t in targets) StringAssert.EndsWith("?b=0123abcd", t);
                Assert.AreEqual(2, server.Hits("content/bundle.json"));
            }
        }
        finally { Directory.Delete(site, true); }
    }

    [Test]
    public void OnlyNoResponseAndServerSideStatusesAreRetried()
    {
        Assert.IsTrue(StreamingFetch.Transient(UnityWebRequest.Result.ConnectionError, 0));
        foreach (var status in new long[] { 500, 502, 503, 504, 408, 429 })
            Assert.IsTrue(StreamingFetch.Transient(UnityWebRequest.Result.ProtocolError, status), status.ToString());
        foreach (var status in new long[] { 400, 403, 404, 410 })
            Assert.IsFalse(StreamingFetch.Transient(UnityWebRequest.Result.ProtocolError, status), status.ToString());
        Assert.IsFalse(StreamingFetch.Transient(UnityWebRequest.Result.DataProcessingError, 200));
        Assert.AreEqual(4, StreamingFetch.MaxAttempts);
        CollectionAssert.AreEqual(new[] { 0.5f, 1f, 2f }, new[] { StreamingFetch.Backoff(1), StreamingFetch.Backoff(2), StreamingFetch.Backoff(3) });
    }

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator ANotFoundFileIsNotRetried()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", ContentManifest.BundleFileName));
        try
        {
            using (var server = new FlakySite(site, new Dictionary<string, (string, int)>()))
            {
                var fetch = GameInputs.FetchAsync(server.BaseUrl).GetAwaiter();
                yield return Await(fetch);
                StringAssert.StartsWith("bundle.json: ", Assert.Throws<IOException>(() => fetch.GetResult()).Message);
                Assert.AreEqual(1, server.Hits("content/bundle.json"));
                Assert.AreEqual(0, StreamingFetch.OpenRequests);
            }
        }
        finally { Directory.Delete(site, true); }
    }

    /// <summary>bundle.json's shape (ContentManifest.BundleJson is private), to write a tampered one.</summary>
    [Serializable] sealed class PackedForTest { public string[] files; public string[] texts; }

    [Test]
    public void VerifyAcceptsACopyAndRejectsAMissingFileOrManifestMismatch()
    {
        var target = Path.Combine(Path.GetTempPath(), "statecraft-verify-" + System.Guid.NewGuid().ToString("N"));
        try
        {
            ContentManifest.Copy(Paths.Content, target);
            ContentManifest.Verify(Paths.Content, target);
            File.Delete(Path.Combine(target, "people.yaml"));
            StringAssert.Contains("people.yaml", Assert.Throws<InvalidDataException>(() => ContentManifest.Verify(Paths.Content, target)).Message);
            ContentManifest.Copy(Paths.Content, target);
            File.WriteAllText(Path.Combine(target, ContentManifest.FileName), ContentManifest.Write(new[] { "people.yaml" }));
            StringAssert.Contains("does not match", Assert.Throws<InvalidDataException>(() => ContentManifest.Verify(Paths.Content, target)).Message);
            ContentManifest.Copy(Paths.Content, target);
            var files = ContentManifest.Collect(Paths.Content);
            var (_, texts) = ContentManifest.ParseBundle(File.ReadAllText(Path.Combine(target, ContentManifest.BundleFileName)));
            texts["people.yaml"] += "\n# edited after the copy\n";
            File.WriteAllText(Path.Combine(target, ContentManifest.BundleFileName),
                JsonUtility.ToJson(new PackedForTest { files = files.ToArray(), texts = files.Select(f => texts[f]).ToArray() }));
            Assert.AreEqual("people.yaml: its text in bundle.json differs from the source",
                Assert.Throws<InvalidDataException>(() => ContentManifest.Verify(Paths.Content, target)).Message);
            File.Delete(Path.Combine(target, ContentManifest.BundleFileName));
            StringAssert.StartsWith("bundle.json: missing", Assert.Throws<InvalidDataException>(() => ContentManifest.Verify(Paths.Content, target)).Message);
        }
        finally { if (Directory.Exists(target)) Directory.Delete(target, true); }
    }
}
