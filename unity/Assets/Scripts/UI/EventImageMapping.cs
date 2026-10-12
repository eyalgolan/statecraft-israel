#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.UI
{
    /// <summary>Reads and checks Assets/UI/EventImages/event-images.yaml, the event -> photograph mapping (a Unity-side
    /// file, never part of the hashed game content). EventImageImport builds EventImageCatalog from it; tests check it.</summary>
    public static class EventImageMapping
    {
        public const string Folder = "Assets/UI/EventImages";
        public const string Path = Folder + "/event-images.yaml";
        /// <summary>The licences an event photograph may carry (owner ruling 2026-10-11): public domain, CC0 or CC BY 4.0.
        /// Spelled as in ASSETS.yaml (scripts/oss/oss_policy.py ALLOWED_LICENCES). Never share-alike, never "unknown".</summary>
        public static readonly IReadOnlyDictionary<string, string> Licences = new Dictionary<string, string>
        {
            ["Public-Domain"] = "public domain", ["CC0-1.0"] = "CC0", ["CC-BY-4.0"] = "CC BY 4.0",
        };
        /// <summary>Every credit names the adaptation, as CC BY 4.0 asks.</summary>
        public const string Adaptation = "graded and cropped";

        public sealed class Row
        {
            public string Event { get; set; } = "";
            public string File { get; set; } = "";
            public string Commons { get; set; } = "";
            public string Original { get; set; } = "";
            public string Sha1 { get; set; } = "";
            public List<int> OriginalSize { get; set; } = new List<int>();
            public string Licence { get; set; } = "";
            public string Artist { get; set; } = "";
            public string Credit { get; set; } = "";
            public string Alt { get; set; } = "";
            public List<double> Crop { get; set; } = new List<double>();
            public bool Sensitive { get; set; }
            public bool SensitivityReviewed { get; set; }
            public string Note { get; set; } = "";
        }

        sealed class Doc
        {
            public string Checked { get; set; } = "";
            public List<Row> Images { get; set; } = new List<Row>();
        }

        /// <summary>Parses the mapping. Throws InvalidOperationException naming the file on malformed YAML or unknown keys.</summary>
        public static IReadOnlyList<Row> Parse(string yaml)
        {
            try
            {
                var doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .WithDuplicateKeyChecking().Build().Deserialize<Doc>(yaml ?? "");
                return doc?.Images ?? new List<Row>();
            }
            catch (YamlDotNet.Core.YamlException e) { throw new InvalidOperationException($"{Path}: {e.Message}", e); }
        }

        /// <summary>Every problem with the rows against the game content (empty when the mapping is sound): unknown or
        /// repeated events, licences outside <see cref="Licences"/>, a credit without the artist, licence or adaptation,
        /// missing alt text or source, and an event with a sensitive tag whose picture is not marked sensitive.</summary>
        public static List<string> Problems(IReadOnlyList<Row> rows, Content content)
        {
            var problems = new List<string>();
            var events = content.Events.ToDictionary(e => e.Id);
            var sensitiveEvents = new HashSet<string>(ReleaseGate.SensitiveEvents(content));   // the events with a sensitive tag
            foreach (var g in rows.GroupBy(r => r.Event).Where(g => g.Count() > 1)) problems.Add($"{g.Key}: more than one row");
            foreach (var r in rows)
            {
                if (!events.TryGetValue(r.Event, out var ev)) { problems.Add($"{r.Event}: no such event"); continue; }
                if (r.File != r.Event + ".jpg") problems.Add($"{r.Event}: file must be {r.Event}.jpg (got '{r.File}')");
                if (!Licences.TryGetValue(r.Licence, out var label)) problems.Add($"{r.Event}: licence '{r.Licence}' is not public domain, CC0 or CC BY 4.0");
                else if (!r.Credit.Contains(label)) problems.Add($"{r.Event}: credit does not name the licence '{label}'");
                if (r.Artist == "" || !r.Credit.Contains(r.Artist)) problems.Add($"{r.Event}: credit does not name the artist '{r.Artist}'");
                if (!r.Credit.Contains(Adaptation)) problems.Add($"{r.Event}: credit does not say '{Adaptation}'");
                if (r.Alt.Trim().Length < 20) problems.Add($"{r.Event}: alt text missing or too short");
                if (!r.Commons.StartsWith("https://commons.wikimedia.org/wiki/File:", StringComparison.Ordinal)) problems.Add($"{r.Event}: commons page missing");
                if (r.Sha1.Length != 40) problems.Add($"{r.Event}: sha1 of the original missing");
                if (r.Crop.Count != 3) problems.Add($"{r.Event}: crop must be [centre x, centre y, width]");
                if (sensitiveEvents.Contains(ev.Id) && !r.Sensitive) problems.Add($"{r.Event}: the event has a sensitive tag, so its picture must be sensitive: true");
                if (r.SensitivityReviewed && !r.Sensitive) problems.Add($"{r.Event}: sensitivity_reviewed without sensitive");
            }
            return problems;
        }
    }
}
