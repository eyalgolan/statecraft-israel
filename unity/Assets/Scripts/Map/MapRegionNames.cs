#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.Map
{
    /// <summary>Sourced presentation names, separate from generated geometry and its runtime cache (C242).
    /// A missing entry keeps the existing English field. Every supplied name awaits the owner's native review.</summary>
    public sealed class MapRegionNames
    {
        public const string FileName = "region_names_he.yaml";
        readonly Dictionary<string, string> names = new Dictionary<string, string>(StringComparer.Ordinal);
        public string Pick(string key, string english, bool hebrew) =>
            hebrew && names.TryGetValue(key, out var value) ? value : english;

        sealed class Source
        {
            public string Url { get; set; } = "";
            public string Checked { get; set; } = "";
            public string Cite { get; set; } = "";
        }
        sealed class Entry
        {
            public string Key { get; set; } = "";
            public string NameHe { get; set; } = "";
            public List<Source> NameHeSources { get; set; } = new List<Source>();
            public string NameHeReview { get; set; } = "";
        }
        sealed class Doc { public List<Entry> Names { get; set; } = new List<Entry>(); }

        public static MapRegionNames Parse(string yaml)
        {
            var result = new MapRegionNames();
            if (string.IsNullOrWhiteSpace(yaml)) return result; // old bundles have no catalog
            Doc? doc;
            try { doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithDuplicateKeyChecking().Build().Deserialize<Doc>(yaml); }
            catch (YamlException e) { throw new InvalidDataException($"{FileName}: {e.Message}", e); }
            if (doc?.Names == null) throw new InvalidDataException($"{FileName}: names is required");
            foreach (var e in doc.Names)
            {
                var field = $"{FileName}: names[{e.Key}]";
                if (string.IsNullOrWhiteSpace(e.Key) || result.names.ContainsKey(e.Key))
                    throw new InvalidDataException($"{field}: missing or duplicate key");
                if (string.IsNullOrWhiteSpace(e.NameHe) || !e.NameHe.Any(c => c >= '\u05d0' && c <= '\u05ea'))
                    throw new InvalidDataException($"{field}: name_he must contain Hebrew");
                if (e.NameHeReview != "native-review-pending")
                    throw new InvalidDataException($"{field}: name_he_review must be native-review-pending");
                if (e.NameHeSources == null || e.NameHeSources.Count == 0)
                    throw new InvalidDataException($"{field}: name_he_sources is required");
                foreach (var source in e.NameHeSources)
                    if (source == null || !Uri.TryCreate(source.Url, UriKind.Absolute, out var url) || url.Scheme != "https"
                        || !DateTime.TryParseExact(source.Checked, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                        throw new InvalidDataException($"{field}: name_he_sources needs an https URL and checked date");
                result.names.Add(e.Key, e.NameHe);
            }
            return result;
        }

        /// <summary>Reject a misspelt or stale target rather than silently dropping its name.</summary>
        public void Validate(IEnumerable<string> keys)
        {
            var allowed = new HashSet<string>(keys, StringComparer.Ordinal);
            foreach (var key in names.Keys)
                if (!allowed.Contains(key)) throw new InvalidDataException($"{FileName}: unknown target '{key}'");
        }
    }
}
