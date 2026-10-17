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
    public IEnumerator AMissingFileNamesTheFile()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", "people.yaml"));
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        yield return Await(fetch);
        var ex = Assert.Throws<IOException>(() => fetch.GetResult());
        StringAssert.StartsWith("people.yaml: ", ex.Message);
        Directory.Delete(site, true);
    }

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator AFailedFetchLeavesNoRequestOpen()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", "people.yaml"));
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        yield return Await(fetch);
        Assert.Throws<IOException>(() => fetch.GetResult());
        Assert.AreEqual(0, GameInputs.OpenRequests, "requests still in flight after a failed fetch were not disposed");
        Directory.Delete(site, true);
    }

    /// <summary>A minimal HTTP/1.0 server over a staged site (one connection per request, like web_check's) that fails
    /// chosen files for their first <c>times</c> requests: "drop" resets the connection before any response (what
    /// macOS did to web_check's server when its listen backlog overflowed on a loaded machine: BOOT FAIL
    /// israel_bonds.yaml), a number answers with that status. HttpListener cannot model a drop: Mono's Abort() sends an
    /// empty 200.</summary>
    sealed class FlakySite : IDisposable
    {
        readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        readonly string root;
        readonly Dictionary<string, (string fault, int times)> faults;
        readonly ConcurrentDictionary<string, int> hits = new ConcurrentDictionary<string, int>();
        readonly ManualResetEventSlim stopped = new ManualResetEventSlim();
        /// <summary>Every request target as sent, query included (e.g. "/content/people.yaml?b=..."), in arrival order.</summary>
        public readonly ConcurrentQueue<string> Targets = new ConcurrentQueue<string>();
        public readonly string BaseUrl;

        public FlakySite(string root, Dictionary<string, (string fault, int times)> faults)
        {
            this.root = root; this.faults = faults;
            listener.Start();
            BaseUrl = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}";
            Task.Run(Loop);
        }

        /// <summary>Requests seen for a site path (e.g. "content/people.yaml").</summary>
        public int Hits(string rel) => hits.TryGetValue(rel, out var n) ? n : 0;

        async Task Loop()
        {
            while (true)
            {
                TcpClient client;
                try { client = await listener.AcceptTcpClientAsync(); }
                catch (Exception) { return; }   // stopped
                _ = Task.Run(() => Serve(client));
            }
        }

        void Serve(TcpClient client)
        {
            using (client)
            {
                var stream = client.GetStream();
                var head = new List<byte>();
                var one = new byte[1];
                while (head.Count < 4 || head[head.Count - 4] != '\r' || head[head.Count - 3] != '\n' || head[head.Count - 2] != '\r' || head[head.Count - 1] != '\n')
                {
                    if (stream.Read(one, 0, 1) == 0) return;
                    head.Add(one[0]);
                }
                var target = System.Text.Encoding.ASCII.GetString(head.ToArray()).Split(' ')[1];
                Targets.Enqueue(target);
                var rel = Uri.UnescapeDataString(target.Split('?')[0].TrimStart('/'));
                var n = hits.AddOrUpdate(rel, 1, (_, v) => v + 1);
                if (faults.TryGetValue(rel, out var f) && n <= f.times)
                {
                    if (f.fault == "drop") { client.Client.LingerState = new LingerOption(true, 0); client.Client.Close(); return; }  // RST
                    if (f.fault == "stall") { stopped.Wait(); return; }   // Accept the request but never answer it.
                    if (f.fault == "trickle") { Trickle(stream, File.ReadAllBytes(Path.Combine(root, rel))); return; }
                    Respond(stream, int.Parse(f.fault), new byte[0]);
                    return;
                }
                var path = Path.Combine(root, rel);
                if (File.Exists(path)) Respond(stream, 200, File.ReadAllBytes(path));
                else Respond(stream, 404, new byte[0]);
            }
        }

        static void Respond(NetworkStream stream, int status, byte[] body)
        {
            var head = System.Text.Encoding.ASCII.GetBytes($"HTTP/1.0 {status} {(status == 200 ? "OK" : "Fault")}\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
            stream.Write(head, 0, head.Length);
            stream.Write(body, 0, body.Length);
        }

        /// <summary>A slow but healthy download: the whole file, in TrickleChunks parts TrickleGapMs apart.</summary>
        static void Trickle(NetworkStream stream, byte[] body)
        {
            var head = System.Text.Encoding.ASCII.GetBytes($"HTTP/1.0 200 OK\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
            stream.Write(head, 0, head.Length);
            var part = (body.Length + TrickleChunks - 1) / TrickleChunks;
            for (var at = 0; at < body.Length; at += part)
            {
                Thread.Sleep(TrickleGapMs);
                stream.Write(body, at, Math.Min(part, body.Length - at));
                stream.Flush();
            }
        }

        public void Dispose() { stopped.Set(); listener.Stop(); }
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

    /// <summary>Waits for a fetch for as long as it makes progress: it fails when GameInputs.OpenRequests (requests
    /// started and not yet released) has not changed for StallSeconds, or once CapSeconds have passed in all. The
    /// manifest's request is released and every file's started, then each file's request is released as the fetch takes
    /// it (a retry releases one and starts another), so the count keeps moving while the fetch works.
    /// A batch-mode editor runs the player loop (which resumes Awaitable.NextFrameAsync) only now and then while idle: a
    /// retry's 0.5 s backoff measured 57 s over 2 frames. Each tick queues a player-loop update, as a player runs one
    /// every frame.</summary>
    static IEnumerator Await(Awaitable<GameInputs>.Awaiter fetch) =>
        WaitWhileProgressing(() => fetch.IsCompleted, () => GameInputs.OpenRequests, StallSeconds, CapSeconds, () => Time.realtimeSinceStartup, "fetch");

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
                Assert.AreEqual(0, GameInputs.OpenRequests);
                check(server);
            }
        }
        finally { Directory.Delete(site, true); }
    }

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator DroppedConnectionsAreRetried() =>
        FetchesTheDiskContentDespite(new Dictionary<string, (string, int)> { ["content/events/israel_bonds.yaml"] = ("drop", 2) },
            server => Assert.AreEqual(3, server.Hits("content/events/israel_bonds.yaml")));

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator AServerErrorIsRetried() =>
        FetchesTheDiskContentDespite(new Dictionary<string, (string, int)> { ["content/people.yaml"] = ("503", 1), ["map/heightmap.r16z"] = ("500", 2) },
            server =>
            {
                Assert.AreEqual(2, server.Hits("content/people.yaml"));
                Assert.AreEqual(3, server.Hits("map/heightmap.r16z"));
            });

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator AConnectionThatKeepsDroppingFailsNamingTheFileWithNothingLeftOpen()
    {
        var site = StagedSite();
        try
        {
            var faults = new Dictionary<string, (string, int)> { ["content/events/israel_bonds.yaml"] = ("drop", int.MaxValue) };
            using (var server = new FlakySite(site, faults))
            {
                var fetch = GameInputs.FetchAsync(server.BaseUrl).GetAwaiter();
                yield return Await(fetch);
                var ex = Assert.Throws<IOException>(() => fetch.GetResult());
                StringAssert.StartsWith("events/israel_bonds.yaml: ", ex.Message);
                StringAssert.EndsWith($"after {GameInputs.MaxAttempts} attempts", ex.Message);
                Assert.AreEqual(GameInputs.MaxAttempts, server.Hits("content/events/israel_bonds.yaml"));
                Assert.AreEqual(0, GameInputs.OpenRequests, "requests still in flight after a failed fetch were not disposed");
            }
        }
        finally { Directory.Delete(site, true); }
    }

    const int TrickleChunks = 10, TrickleGapMs = 300;

    /// <summary>#911: the stall limit is on silence, not on the whole download. A file that takes 3 s to arrive, a part every
    /// 0.3 s, passes a 2 s stall limit at its first attempt (a fixed per-request timeout would cut it off).</summary>
    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator ASlowDownloadThatKeepsReceivingIsNotCutOff()
    {
        var site = StagedSite();
        try
        {
            var faults = new Dictionary<string, (string, int)> { ["content/manifest.json"] = ("trickle", 1) };
            using (var server = new FlakySite(site, faults))
            {
                var started = Time.realtimeSinceStartup;
                var fetch = GameInputs.FetchAsync(server.BaseUrl, "", 2f).GetAwaiter();
                yield return Await(fetch);
                var fetched = fetch.GetResult();
                Assert.AreEqual(GameInputs.FromDisk(Paths.Content, Paths.StreamingMap).Content.Hash, fetched.Content.Hash);
                Assert.AreEqual(1, server.Hits("content/manifest.json"), "the slow download was aborted and retried");
                Assert.Greater(Time.realtimeSinceStartup - started, TrickleChunks * TrickleGapMs / 1000f, "the download was slower than the stall limit");
                Assert.AreEqual(0, GameInputs.OpenRequests);
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
            var faults = new Dictionary<string, (string, int)> { ["content/manifest.json"] = ("stall", int.MaxValue) };
            using (var server = new FlakySite(site, faults))
            {
                var boot = bootObject.AddComponent<GameBoot>();
                string failure = null;
                boot.Failed += reason => failure = reason;
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("BOOT FAIL manifest.json: .* after 4 attempts"));
                LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("manifest.json: .* after 4 attempts"));

                var started = Time.realtimeSinceStartup;
                var run = boot.BootAsync(() => GameInputs.FetchAsync(server.BaseUrl, "", testStallSeconds)).GetAwaiter();
                while (!run.IsCompleted)
                {
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                    yield return null;
                }
                run.GetResult();

                StringAssert.StartsWith("manifest.json: ", failure);
                StringAssert.EndsWith($"after {GameInputs.MaxAttempts} attempts", failure);
                Assert.AreEqual(GameInputs.MaxAttempts, server.Hits("content/manifest.json"));
                Assert.Less(Time.realtimeSinceStartup - started,
                    GameInputs.MaxAttempts * testStallSeconds + 10f, "boot failure was not visible within the stated retry time");
                Assert.AreEqual(0, GameInputs.OpenRequests);
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

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator EveryFetchCarriesTheBuildIdRetriesIncluded()
    {
        var site = StagedSite();
        try
        {
            var faults = new Dictionary<string, (string, int)> { ["content/people.yaml"] = ("503", 1) };
            using (var server = new FlakySite(site, faults))
            {
                var fetch = GameInputs.FetchAsync(server.BaseUrl, "0123abcd").GetAwaiter();
                yield return Await(fetch);
                fetch.GetResult();
                var targets = server.Targets.ToArray();
                var manifest = ContentManifest.Collect(Paths.Content);
                Assert.AreEqual(manifest.Count + 4, targets.Length);   // the manifest, every file, heightmap.json and .r16z, one retry
                foreach (var t in targets) StringAssert.EndsWith("?b=0123abcd", t);
                Assert.AreEqual(2, server.Hits("content/people.yaml"));
            }
        }
        finally { Directory.Delete(site, true); }
    }

    [Test]
    public void OnlyNoResponseAndServerSideStatusesAreRetried()
    {
        Assert.IsTrue(GameInputs.Transient(UnityWebRequest.Result.ConnectionError, 0));
        foreach (var status in new long[] { 500, 502, 503, 504, 408, 429 })
            Assert.IsTrue(GameInputs.Transient(UnityWebRequest.Result.ProtocolError, status), status.ToString());
        foreach (var status in new long[] { 400, 403, 404, 410 })
            Assert.IsFalse(GameInputs.Transient(UnityWebRequest.Result.ProtocolError, status), status.ToString());
        Assert.IsFalse(GameInputs.Transient(UnityWebRequest.Result.DataProcessingError, 200));
        Assert.AreEqual(4, GameInputs.MaxAttempts);
        CollectionAssert.AreEqual(new[] { 0.5f, 1f, 2f }, new[] { GameInputs.Backoff(1), GameInputs.Backoff(2), GameInputs.Backoff(3) });
    }

    [UnityTest, Timeout(FetchTestTimeoutMs)]
    public IEnumerator ANotFoundFileIsNotRetried()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", "people.yaml"));
        try
        {
            using (var server = new FlakySite(site, new Dictionary<string, (string, int)>()))
            {
                var fetch = GameInputs.FetchAsync(server.BaseUrl).GetAwaiter();
                yield return Await(fetch);
                StringAssert.StartsWith("people.yaml: ", Assert.Throws<IOException>(() => fetch.GetResult()).Message);
                Assert.AreEqual(1, server.Hits("content/people.yaml"));
                Assert.AreEqual(0, GameInputs.OpenRequests);
            }
        }
        finally { Directory.Delete(site, true); }
    }

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
        }
        finally { if (Directory.Exists(target)) Directory.Delete(target, true); }
    }
}
