#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.UI
{
    /// <summary>Reads and checks Assets/Resources/EventVideos/event-videos.yaml, the event -> illustration clip mapping
    /// (decision videos, owner rulings 2026-10-13; docs/event-videos.md). A Unity-side file, never part of the hashed game
    /// content. The game reads it from Resources at start (Load); the clips themselves are in StreamingAssets/video and are
    /// fetched by URL only when the player asks for one (CardVideo).</summary>
    public static class EventVideoMapping
    {
        public const string ResourcePath = "EventVideos/event-videos";
        public const string Path = "Assets/Resources/EventVideos/event-videos.yaml";
        public const string ClipFolder = "Assets/StreamingAssets/video";

        public sealed class Row
        {
            public string Event { get; set; } = "";
            public string File { get; set; } = "";
            public string Shot { get; set; } = "";
            public string Source { get; set; } = "";
            public string SourceSha256 { get; set; } = "";
            public string Generator { get; set; } = "";
            public string Generated { get; set; } = "";
            public string Alt { get; set; } = "";
            public bool AiGenerated { get; set; }
            public bool Sensitive { get; set; }
            public bool SensitivityReviewed { get; set; }
        }

        sealed class Doc
        {
            public string Checked { get; set; } = "";
            public List<Row> Clips { get; set; } = new List<Row>();
        }

        /// <summary>Parses the mapping. Throws InvalidOperationException naming the file on malformed YAML or unknown keys.</summary>
        public static IReadOnlyList<Row> Parse(string yaml)
        {
            try
            {
                var doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .WithDuplicateKeyChecking().Build().Deserialize<Doc>(yaml ?? "");
                return doc?.Clips ?? new List<Row>();
            }
            catch (YamlDotNet.Core.YamlException e) { throw new InvalidOperationException($"{Path}: {e.Message}", e); }
        }

        /// <summary>The shipped mapping, or no rows when it is missing.</summary>
        public static IReadOnlyList<Row> Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            return asset == null ? Array.Empty<Row>() : Parse(asset.text);
        }

        /// <summary>Every problem with the rows (empty when the mapping is sound): an unknown or repeated event; an event
        /// with a deport option (Deport never gets any video: GDD owner ruling 1, CONTENT_POLICY section 4); an event with no
        /// archival photograph (a clip is never the only picture of a real event); a file that is not &lt;event&gt;.mp4 or is
        /// missing; a row not marked ai_generated, or without its source, SHA-256, generator, date or alt text; and an event
        /// with a sensitive tag whose clip is not marked sensitive.</summary>
        public static List<string> Problems(IReadOnlyList<Row> rows, Content content, Func<string, bool> hasPhoto, Func<string, bool> fileExists)
        {
            var problems = new List<string>();
            var events = content.Events.ToDictionary(e => e.Id);
            var sensitiveEvents = new HashSet<string>(ReleaseGate.SensitiveEvents(content));
            foreach (var g in rows.GroupBy(r => r.Event).Where(g => g.Count() > 1)) problems.Add($"{g.Key}: more than one row");
            foreach (var r in rows)
            {
                if (!events.TryGetValue(r.Event, out var ev)) { problems.Add($"{r.Event}: no such event"); continue; }
                if (ev.Options.Any(o => o.Id.IndexOf("deport", StringComparison.OrdinalIgnoreCase) >= 0))
                    problems.Add($"{r.Event}: has a deport option; Deport never gets any video");
                if (!hasPhoto(r.Event)) problems.Add($"{r.Event}: has no archival photograph; a clip is never the only picture of an event");
                if (r.File != r.Event + ".mp4") problems.Add($"{r.Event}: file must be {r.Event}.mp4 (got '{r.File}')");
                else if (!fileExists(r.File)) problems.Add($"{r.Event}: {ClipFolder}/{r.File} missing (run tools/encode_event_videos.py)");
                if (!r.AiGenerated) problems.Add($"{r.Event}: ai_generated must be true");
                if (!r.Source.StartsWith("promo/", StringComparison.Ordinal)) problems.Add($"{r.Event}: source must be a promo shot");
                if (r.SourceSha256.Length != 64) problems.Add($"{r.Event}: source_sha256 missing");
                if (r.Generator.Trim() == "" || r.Generated.Trim() == "") problems.Add($"{r.Event}: generator or date missing");
                if (!r.Alt.StartsWith("Illustration: ", StringComparison.Ordinal) || r.Alt.Length < 30) problems.Add($"{r.Event}: alt text must start 'Illustration: ' and say what it shows");
                if (sensitiveEvents.Contains(ev.Id) && !r.Sensitive) problems.Add($"{r.Event}: the event has a sensitive tag, so its clip must be sensitive: true");
                if (r.SensitivityReviewed && !r.Sensitive) problems.Add($"{r.Event}: sensitivity_reviewed without sensitive");
            }
            return problems;
        }

        /// <summary>The events whose clip is sensitive and not yet sensitivity-reviewed, in id order.</summary>
        public static IReadOnlyList<string> SensitiveUnreviewed(IReadOnlyList<Row> rows) =>
            rows.Where(r => r.Sensitive && !r.SensitivityReviewed).Select(r => r.Event).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }
}
