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
        /// Memory (review M10): loading the catalog pulls in all its textures at start and they are never unloaded. At 24
        /// pictures of at most 960x540 (the 16:9 media header), DXT1, crunched, no mipmaps, that is at most about 260 KB each
        /// and 6.2 MB in all, which is fine. If the count grows past about 50, keep a Resources path per entry instead and
        /// load each event's texture on demand.</summary>
        public static EventImageCatalog? Load() => Resources.Load<EventImageCatalog>(ResourcePath);

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
}
