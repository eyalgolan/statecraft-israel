#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Streams The 1948 Collection's models after the first frame (issue #664; owner, 15 October 2026: "we need to
    /// make sure this is built in a way that doesnt make the game extremely hard to load"). Ask for a model with
    /// <see cref="Request"/>: nothing comes back at once, so the caller draws its placeholder (or nothing), and the callback
    /// later gets the model's prefab to instantiate. Nothing is fetched until the frame after the first request, so a
    /// request never delays the frame that made it, and nothing is fetched at all until something asks.
    /// It fails open: a bundle that is missing, damaged or will not load logs one warning, every request for it gets null,
    /// and the map runs on without those models. An id the collection does not have gets null and one warning. An exception
    /// from the source or from a caller's callback is caught for that request alone: it logs one warning (once per bundle,
    /// prefab or id) and the stream goes on serving every later request.
    /// The player reads the bundles from StreamingAssets/<see cref="CollectionCatalog.StreamingFolder"/> (every Build.*
    /// player build, Web, Mac, Linux and Windows, puts them there); the editor reads the prefabs from the AssetDatabase, so map work needs no bundle build.</summary>
    public sealed class CollectionStream
    {
        /// <summary>Where the models come from: bundles in a build, the AssetDatabase in the editor, a fake in tests.</summary>
        public interface ISource
        {
            /// <summary>Opens <paramref name="bundle"/>; calls <paramref name="done"/> with null on success or with why it failed.</summary>
            IEnumerator Open(string bundle, Action<string?> done);
            /// <summary>Loads the prefab at <paramref name="path"/> from the opened <paramref name="bundle"/> (null if absent).</summary>
            IEnumerator Load(string bundle, string path, Action<GameObject?> done);
        }

        enum State { Closed, Open, Failed }

        readonly ISource source;
        readonly Dictionary<string, State> bundles = new Dictionary<string, State>(StringComparer.Ordinal);
        readonly Dictionary<string, GameObject?> loaded = new Dictionary<string, GameObject?>(StringComparer.Ordinal);
        readonly HashSet<string> warnedIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> warnedCallbacks = new HashSet<string>(StringComparer.Ordinal);
        readonly Queue<(string Id, CollectionDetail Detail, Action<GameObject?> Done)> pending =
            new Queue<(string, CollectionDetail, Action<GameObject?>)>();

        public CollectionStream(ISource source) { this.source = source; }

        /// <summary>The game's stream, made on the first request with the source for this player (or the editor).</summary>
        static CollectionStream? shared;
        static CollectionStreamHost? host;

        /// <summary>Asks for model <paramref name="id"/> at <paramref name="detail"/>; <paramref name="done"/> runs on a later
        /// frame with the prefab, or with null when the model cannot be had (fail open).</summary>
        public static void Request(string id, CollectionDetail detail, Action<GameObject?> done)
        {
            shared ??= new CollectionStream(DefaultSource());
            if (shared.Enqueue(id, detail, done) && host == null)
            {
                var go = new GameObject("CollectionStream") { hideFlags = HideFlags.HideAndDontSave };
                if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
                host = go.AddComponent<CollectionStreamHost>();
                host.StartCoroutine(host.Run(shared));
            }
        }

        static ISource DefaultSource()
        {
#if UNITY_EDITOR
            return new EditorSource();
#else
            return new BundleSource(Path.Combine(Application.streamingAssetsPath, CollectionCatalog.StreamingFolder));
#endif
        }

        /// <summary>Queues a request; false (and <paramref name="done"/> already called with null) for an unknown id.</summary>
        public bool Enqueue(string id, CollectionDetail detail, Action<GameObject?> done)
        {
            if (CollectionCatalog.BundleOf(id) == null)
            {
                if (warnedIds.Add(id)) Debug.LogWarning($"Collection: no model '{id}' in The 1948 Collection; nothing is drawn for it.");
                done(null);
                return false;
            }
            pending.Enqueue((id, detail, done));
            return true;
        }

        /// <summary>Serves the queued requests, one frame after it starts, then each new one as it comes, for ever (the host
        /// runs it as a coroutine; tests step it by hand).</summary>
        public IEnumerator Run()
        {
            yield return null;   // after the frame that made the first request
            while (true)
            {
                while (pending.Count > 0)
                {
                    var (id, detail, done) = pending.Dequeue();
                    var bundle = CollectionCatalog.BundleOf(id)!;
                    if (!bundles.TryGetValue(bundle, out var state))
                    {
                        string? error = null;
                        yield return Guard(() => source.Open(bundle, e => error = e), e => error = e);
                        state = error == null ? State.Open : State.Failed;
                        bundles[bundle] = state;
                        if (error != null)
                            Debug.LogWarning($"Collection: bundle {bundle} did not load ({error}); its models stay off the map.");
                    }
                    if (state != State.Open) { Answer(id, done, null); continue; }
                    var path = CollectionCatalog.PrefabPath(id, detail);
                    if (!loaded.TryGetValue(path, out var prefab))
                    {
                        string? error = null;
                        yield return Guard(() => source.Load(bundle, path, p => prefab = p), e => error = e);
                        if (error != null) prefab = null;
                        loaded[path] = prefab;
                        if (error != null) Debug.LogWarning($"Collection: bundle {bundle} did not load {path} ({error}); nothing is drawn for it.");
                        else if (prefab == null) Debug.LogWarning($"Collection: bundle {bundle} has no {path}; nothing is drawn for it.");
                    }
                    Answer(id, done, prefab);
                }
                yield return null;
            }
        }

        /// <summary>Hands <paramref name="prefab"/> to the caller; a callback that throws logs one warning per id and never
        /// stops the stream.</summary>
        void Answer(string id, Action<GameObject?> done, GameObject? prefab)
        {
            try { done(prefab); }
            catch (Exception e)
            {
                if (warnedCallbacks.Add(id))
                    Debug.LogWarning($"Collection: the callback for '{id}' threw ({Describe(e)}); the stream serves the next request.");
            }
        }

        static string Describe(Exception e) => e.GetType().Name + ": " + e.Message;

        /// <summary>Runs the source's coroutine (and any it nests) so that an exception, thrown when it starts or at any step,
        /// ends only that coroutine and reaches <paramref name="failed"/>, instead of ending <see cref="Run"/> with it. (C#
        /// cannot yield inside a try with a catch, so each step is taken inside one and yielded outside it.)</summary>
        static IEnumerator Guard(Func<IEnumerator> start, Action<string> failed)
        {
            IEnumerator inner;
            try { inner = start(); }
            catch (Exception e) { failed(Describe(e)); inner = Done(); }
            while (true)
            {
                object? current = null;
                bool more;
                try
                {
                    more = inner.MoveNext();
                    if (more) current = inner.Current;
                }
                catch (Exception e) { failed(Describe(e)); more = false; }
                if (!more) yield break;
                yield return current is IEnumerator nested ? Guard(() => nested, failed) : current;
            }
        }

        static IEnumerator Done() { yield break; }

        /// <summary>Reads the bundles from a folder: by URL on WebGL (StreamingAssets is served beside the page), from disk
        /// elsewhere. Each bundle must match its line of <see cref="CollectionCatalog.ChecksFile"/>, written by the bundle
        /// build: Unity is given the bundle's CRC on both paths, and on disk a missing file, one that is not a whole
        /// AssetBundle by its header (<see cref="CheckHeader"/>) or one whose size or SHA-256 differs from the build fails at
        /// once without asking Unity to open it, so it logs this class's one warning and not Unity's own errors too.</summary>
        public sealed class BundleSource : ISource
        {
            readonly string folder;
            readonly Dictionary<string, AssetBundle> open = new Dictionary<string, AssetBundle>(StringComparer.Ordinal);
            Dictionary<string, CollectionCatalog.BundleCheck>? checks;
            string? checksError;
            bool checksRead;

            public BundleSource(string folder) { this.folder = folder; }

            public IEnumerator Open(string bundle, Action<string?> done)
            {
                var path = Path.Combine(folder, bundle);
                AssetBundle? b = null;
                if (path.Contains("://"))
                {
                    yield return ReadChecks();
                    var (check, why) = Lookup(bundle);
                    if (check == null) { done(why); yield break; }
                    // Unity checks the CRC as it reads, and refuses a bundle that does not match (the request then fails).
                    using var req = UnityWebRequestAssetBundle.GetAssetBundle(folder + "/" + bundle, check.Value.Crc);
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success) { done(req.error ?? "request failed"); yield break; }
                    b = DownloadHandlerAssetBundle.GetContent(req);
                }
                else
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    done("WebGL reads the bundles by URL, not from disk " + path); yield break;   // keeps SHA-256 out of the wasm
#else
                    if (!File.Exists(path)) { done("no file " + path); yield break; }
                    var damaged = CheckHeader(path);
                    if (damaged != null) { done(damaged + " " + path); yield break; }
                    yield return ReadChecks();
                    var (check, why) = Lookup(bundle);
                    if (check == null) { done(why); yield break; }
                    // The whole file against the build's size and SHA-256 first, so a damaged bundle never reaches Unity's
                    // reader (which logs errors of its own for one, and crashed the editor on a cut-short one).
                    if (new FileInfo(path).Length != check.Value.Bytes || CollectionCatalog.Sha256(path) != check.Value.Sha256)
                    { done("it does not match the build that made it: " + path); yield break; }
                    var op = AssetBundle.LoadFromFileAsync(path, check.Value.Crc);
                    yield return op;
                    b = op.assetBundle;
#endif
                }
                if (b == null) { done("not an AssetBundle"); yield break; }
                open[bundle] = b;
                done(null);
            }

            /// <summary>Reads <see cref="CollectionCatalog.ChecksFile"/> once (by URL or from disk, like the bundles).</summary>
            IEnumerator ReadChecks()
            {
                if (checksRead) yield break;
                checksRead = true;
                string? text = null;
                if (folder.Contains("://"))
                {
                    using var req = UnityWebRequest.Get(folder + "/" + CollectionCatalog.ChecksFile);
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) text = req.downloadHandler.text;
                    else checksError = $"no {CollectionCatalog.ChecksFile} ({req.error})";
                }
                else
                {
                    var file = Path.Combine(folder, CollectionCatalog.ChecksFile);
                    if (File.Exists(file)) text = File.ReadAllText(file);
                    else checksError = "no file " + file;
                }
                if (text != null && (checks = CollectionCatalog.ParseChecks(text)) == null)
                    checksError = $"a damaged {CollectionCatalog.ChecksFile}";
            }

            /// <summary>The bundle's line of the checks file, or null and why not.</summary>
            (CollectionCatalog.BundleCheck? Check, string Why) Lookup(string bundle)
            {
                if (checks == null) return (null, checksError ?? "no " + CollectionCatalog.ChecksFile);
                return checks.TryGetValue(bundle, out var c) ? (c, "") : (null, $"not in {CollectionCatalog.ChecksFile}");
            }

            public IEnumerator Load(string bundle, string path, Action<GameObject?> done)
            {
                var op = open[bundle].LoadAssetAsync<GameObject>(path);
                yield return op;
                done(op.asset as GameObject);
            }

            /// <summary>Null when the file at <paramref name="path"/> starts with an AssetBundle header ("UnityFS", format
            /// version, two version strings, then the whole file's size, big-endian) whose size is the file's own; else why
            /// not. (Unity's own reader would log an error for such a file, on top of this class's warning.)</summary>
            public static string? CheckHeader(string path)
            {
                try
                {
                    using var f = File.OpenRead(path);
                    using var r = new BinaryReader(f);
                    string Text()
                    {
                        var b = new List<byte>();
                        for (int c; (c = f.ReadByte()) > 0 && b.Count < 64;) b.Add((byte)c);
                        return System.Text.Encoding.ASCII.GetString(b.ToArray());
                    }
                    if (Text() != "UnityFS") return "not an AssetBundle";
                    if (f.Length < f.Position + 4) return "not an AssetBundle";
                    r.ReadBytes(4);   // format version
                    Text();           // engine versions
                    Text();
                    var size = r.ReadBytes(8);
                    if (size.Length < 8) return "not an AssetBundle";
                    long declared = 0;
                    foreach (var b in size) declared = (declared << 8) | b;
                    return declared == f.Length ? null : $"a damaged AssetBundle ({f.Length} bytes, its header says {declared})";
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { return "unreadable (" + e.Message + ")"; }
            }

            /// <summary>Unloads every bundle this source opened, and the objects loaded from them.</summary>
            public void UnloadAll()
            {
                foreach (var b in open.Values) b.Unload(true);
                open.Clear();
            }
        }

#if UNITY_EDITOR
        /// <summary>The editor's source: the prefabs as they stand in the project.</summary>
        public sealed class EditorSource : ISource
        {
            public IEnumerator Open(string bundle, Action<string?> done) { done(null); yield break; }

            public IEnumerator Load(string bundle, string path, Action<GameObject?> done)
            {
                done(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path));
                yield break;
            }
        }
#endif
    }
}
