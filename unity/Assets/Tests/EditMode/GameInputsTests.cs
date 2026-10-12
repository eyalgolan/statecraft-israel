using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

    [UnityTest]
    public IEnumerator FetchingOverUrlsLoadsTheSameContentAsTheDisk()
    {
        var site = StagedSite();
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        var start = Time.realtimeSinceStartup;
        while (!fetch.IsCompleted && Time.realtimeSinceStartup - start < 60f) yield return null;
        Assert.IsTrue(fetch.IsCompleted, "fetch did not finish within 60 seconds");
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

    [UnityTest]
    public IEnumerator AMissingFileNamesTheFile()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", "people.yaml"));
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        var start = Time.realtimeSinceStartup;
        while (!fetch.IsCompleted && Time.realtimeSinceStartup - start < 60f) yield return null;
        Assert.IsTrue(fetch.IsCompleted, "fetch did not finish within 60 seconds");
        var ex = Assert.Throws<IOException>(() => fetch.GetResult());
        StringAssert.StartsWith("people.yaml: ", ex.Message);
        Directory.Delete(site, true);
    }

    [UnityTest]
    public IEnumerator AFailedFetchLeavesNoRequestOpen()
    {
        var site = StagedSite();
        File.Delete(Path.Combine(site, "content", "people.yaml"));
        var fetch = GameInputs.FetchAsync(new System.Uri(site).AbsoluteUri.TrimEnd('/')).GetAwaiter();
        var start = Time.realtimeSinceStartup;
        while (!fetch.IsCompleted && Time.realtimeSinceStartup - start < 60f) yield return null;
        Assert.IsTrue(fetch.IsCompleted, "fetch did not finish within 60 seconds");
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
                var rel = Uri.UnescapeDataString(target.Split('?')[0].TrimStart('/'));
                var n = hits.AddOrUpdate(rel, 1, (_, v) => v + 1);
                if (faults.TryGetValue(rel, out var f) && n <= f.times)
                {
                    if (f.fault == "drop") { client.Client.LingerState = new LingerOption(true, 0); client.Client.Close(); return; }  // RST
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

        public void Dispose() => listener.Stop();
    }

    /// <summary>Waits up to 60 s for a fetch. A batch-mode editor runs the player loop (which resumes
    /// Awaitable.NextFrameAsync) only now and then while idle: a retry's 0.5 s backoff measured 57 s over 2 frames. Each
    /// tick queues a player-loop update, as a player runs one every frame.</summary>
    static IEnumerator Await(Awaitable<GameInputs>.Awaiter fetch)
    {
        var start = Time.realtimeSinceStartup;
        while (!fetch.IsCompleted && Time.realtimeSinceStartup - start < 60f)
        {
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            yield return null;
        }
        Assert.IsTrue(fetch.IsCompleted, "fetch did not finish within 60 seconds");
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

    [UnityTest]
    public IEnumerator DroppedConnectionsAreRetried() =>
        FetchesTheDiskContentDespite(new Dictionary<string, (string, int)> { ["content/events/israel_bonds.yaml"] = ("drop", 2) },
            server => Assert.AreEqual(3, server.Hits("content/events/israel_bonds.yaml")));

    [UnityTest]
    public IEnumerator AServerErrorIsRetried() =>
        FetchesTheDiskContentDespite(new Dictionary<string, (string, int)> { ["content/people.yaml"] = ("503", 1), ["map/heightmap.r16z"] = ("500", 2) },
            server =>
            {
                Assert.AreEqual(2, server.Hits("content/people.yaml"));
                Assert.AreEqual(3, server.Hits("map/heightmap.r16z"));
            });

    [UnityTest]
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

    [UnityTest]
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
