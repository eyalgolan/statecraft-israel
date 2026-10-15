#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Statecraft.Game.UI
{
    /// <summary>Event id -> the card's header photograph (an archival photo, never an AI image), with its alt text, its
    /// credit line and its sensitivity flags. Built by the editor tool EventImageImport from
    /// Assets/UI/EventImages/event-images.yaml, which is a Unity-side file and NOT game content: the event YAMLs feed the
    /// content hash and save compatibility, and the pictures must never touch either. Kept in Resources so the game loads
    /// it without a scene reference; the textures it references ship in the build's data file.</summary>
    [CreateAssetMenu(menuName = "Statecraft/Event Image Catalog")]
    public sealed class EventImageCatalog : ScriptableObject
    {
        /// <summary>Resources.Load path (Assets/Resources/EventImageCatalog.asset).</summary>
        public const string ResourcePath = "EventImageCatalog";

        [Serializable]
        public struct Entry
        {
            public string eventId; public Texture2D image;
            /// <summary>What the picture shows, for accessibility (the element's tooltip).</summary>
            public string alt;
            /// <summary>The credit line shown under the picture (artist, archive, licence, "graded and cropped").</summary>
            public string credit;
            /// <summary>The picture needs the owner's sensitivity review before a shareable build may carry it.</summary>
            public bool sensitive;
            public bool sensitivityReviewed;
        }

        public List<Entry> entries = new List<Entry>();

        /// <summary>The catalog the game ships, or null when there is none (the card then shows the minister's portrait).
        /// Memory (review M10): loading the catalog pulls in all its textures (EventPhotos: in the start screen's idle time, not at boot) and they are never unloaded. At 24
        /// pictures of at most 960x540 (the 16:9 media header), DXT1, crunched, no mipmaps, that is at most about 260 KB each
        /// and 6.2 MB in all, which is fine. If the count grows past about 50, keep a Resources path per entry instead and
        /// load each event's texture on demand.</summary>
        public static EventImageCatalog? Load()
        {
#if UNITY_EDITOR
            ResourceLoads++;
#endif
            return Resources.Load<EventImageCatalog>(ResourcePath);
        }

        /// <summary>Starts loading the catalog in the background (Resources.LoadAsync); the call it returns gives the catalog,
        /// waiting for the load when it is not done yet (ResourceRequest.asset).</summary>
        public static Func<EventImageCatalog?> LoadAsync()
        {
#if UNITY_EDITOR
            ResourceLoads++;
#endif
            var request = Resources.LoadAsync<EventImageCatalog>(ResourcePath);
            return () => request.asset as EventImageCatalog;
        }

#if UNITY_EDITOR
        /// <summary>Loads from Resources this domain (Load and LoadAsync), for the boot-path test; not in a player.</summary>
        internal static int ResourceLoads { get; private set; }
#endif

        /// <summary>The event's entry, or null when the event has no picture.</summary>
        public Entry? Find(string? eventId)
        {
            if (eventId == null) return null;
            foreach (var e in entries) if (e.eventId == eventId && e.image != null) return e;
            return null;
        }

        /// <summary>The events whose picture is sensitive and not yet sensitivity-reviewed, in id order.</summary>
        public IReadOnlyList<string> SensitiveUnreviewed() =>
            entries.Where(e => e.sensitive && !e.sensitivityReviewed).Select(e => e.eventId).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }
    /// <summary>The shipped catalog, loaded once a page and, for a new game, off the boot path (issue #321: loading it pulls
    /// in its 24 crunched textures, about 101 ms at cpu 4). GameRoot schedules an async load (ScheduleOn, Prefetch) a moment
    /// after the game starts, while the start screen is up; the card asks for the catalog (Get) only when it shows an event.
    /// Get waits for a load still in flight, or loads now when none was started, so the first card always has its
    /// photograph and never the minister's portrait in its place. New Game reuses the loaded catalog.
    /// A new game shows no card at turn 0, so its boot loads nothing. A Continue resumes the autosave inside GameRoot.Begin,
    /// and its turn may have a decision waiting; the card is then built under the start screen and asks for the catalog
    /// only when the screen closes (issue #592), by when the prefetch has usually finished (?bootlog: "prefetch").</summary>
    public static class EventPhotos
    {
        /// <summary>How long after GameRoot.Begin the prefetch starts: clear of the first frames and their shader compiles.</summary>
        public const long PrefetchDelayMs = 1000;
        /// <summary>Test seam: schedules the action on the root after the delay (ms). Only in play mode: an EditMode panel
        /// does not tick, and a test replaces it to see what GameRoot schedules.</summary>
        internal static Action<UnityEngine.UIElements.VisualElement, Action, long> Schedule = DefaultSchedule;

        static void DefaultSchedule(UnityEngine.UIElements.VisualElement root, Action action, long delayMs)
        {
            if (Application.isPlaying) root.schedule.Execute(action).StartingIn(delayMs);
        }

        /// <summary>GameRoot.Begin: the prefetch, PrefetchDelayMs from now, in the start screen's idle time.</summary>
        public static void ScheduleOn(UnityEngine.UIElements.VisualElement root) => Schedule(root, Prefetch, PrefetchDelayMs);

        /// <summary>Test seams: start the async load (returns the call that waits for it and gives the catalog), and load now.</summary>
        internal static Func<Func<EventImageCatalog?>> StartLoad = EventImageCatalog.LoadAsync;
        internal static Func<EventImageCatalog?> LoadNow = EventImageCatalog.Load;
        static Func<EventImageCatalog?>? pending;
        static EventImageCatalog? catalog;
        /// <summary>Loads started (async or not) since the last Reset: one a page.</summary>
        internal static int Loads { get; private set; }
        /// <summary>Get has the catalog (or knows there is none).</summary>
        public static bool Loaded { get; private set; }

        /// <summary>Starts the async load, unless one was started or the catalog is loaded.</summary>
        public static void Prefetch()
        {
            if (Loaded || pending != null) return;
            Loads++;
            pending = StartLoad();
        }

        /// <summary>The catalog, or null when the game ships none or the load gives nothing; the first call finishes the
        /// prefetch or loads it. A null result stays for the page (no retry: the same Resources path gives the same nothing),
        /// and the card falls back to the minister's portrait.</summary>
        public static EventImageCatalog? Get()
        {
            if (Loaded) return catalog;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var how = pending != null ? "prefetch" : "on demand";
            if (pending == null) { Loads++; catalog = LoadNow(); }
            else catalog = pending();
            pending = null; Loaded = true;
            if (Statecraft.Game.Boot.DiagLog.Boot)
                Debug.Log(FormattableString.Invariant($"BOOT event photos ready ({how}) wait={watch.Elapsed.TotalMilliseconds:0} ms"));
            return catalog;
        }

        /// <summary>Tests: forgets the catalog and puts the real loads back.</summary>
        internal static void Reset()
        {
            pending = null; catalog = null; Loaded = false; Loads = 0;
            StartLoad = EventImageCatalog.LoadAsync; LoadNow = EventImageCatalog.Load;
            Schedule = DefaultSchedule;
        }
    }
}
