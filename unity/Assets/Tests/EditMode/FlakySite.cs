using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

/// <summary>A minimal HTTP/1.0 server over a staged site (one connection per request, like web_check's) that fails
/// chosen files for their first <c>times</c> requests: "drop" resets the connection before any response (what
/// macOS did to web_check's server when its listen backlog overflowed on a loaded machine: BOOT FAIL
/// israel_bonds.yaml), a number answers with that status. HttpListener cannot model a drop: Mono's Abort() sends an
/// empty 200. A status may carry one header line after a '|' (e.g. "429|Retry-After: 1"). Shared by the boot's fetch
/// tests (GameInputsTests) and the collection's (CollectionStreamTests).</summary>
sealed class FlakySite : IDisposable
{
    public const int TrickleChunks = 10, TrickleGapMs = 300;

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
                var status = f.fault.Split('|');
                Respond(stream, int.Parse(status[0]), new byte[0], status.Length > 1 ? status[1] : null);
                return;
            }
            var path = Path.Combine(root, rel);
            if (File.Exists(path)) Respond(stream, 200, File.ReadAllBytes(path));
            else Respond(stream, 404, new byte[0]);
        }
    }

    static void Respond(NetworkStream stream, int status, byte[] body, string header = null)
    {
        var extra = header == null ? "" : header + "\r\n";
        var head = System.Text.Encoding.ASCII.GetBytes($"HTTP/1.0 {status} {(status == 200 ? "OK" : "Fault")}\r\nContent-Length: {body.Length}\r\n{extra}Connection: close\r\n\r\n");
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
