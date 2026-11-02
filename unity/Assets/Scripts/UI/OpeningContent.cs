#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.UI
{
    /// <summary>#530: references the existing opening YAML and photographs, without copying them into game content.
    /// One strict reader serves the presenter and the shareable-build gate (Build.OpeningShareableRefusal); it writes no review marker.</summary>
    public sealed class OpeningContent : ScriptableObject
    {
        public const string ResourcePath = "OpeningContent";
        public const string SourcePath = "Assets/UI/Opening/opening.yaml";
        public TextAsset source = null!;
        public Texture2D[] photos = Array.Empty<Texture2D>();
        public static OpeningContent? Load() => Resources.Load<OpeningContent>(ResourcePath);

        public sealed class Source
        {
            public string Url { get; set; } = "";
            public string Supports { get; set; } = "";
            public string Checked { get; set; } = "";
            public bool IsChecked => !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(Supports) && DateTime.TryParseExact(Checked,
                "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }
        public sealed class Arrival
        {
            public string Country { get; set; } = "";
            public int Count { get; set; }
        }
        public sealed class Image
        {
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
        /// <summary>A panel's optional illustration loop (owner ruling of 2 November 2026, "B: A + one new Yemen shot
        /// (~2,424)"): a silent clip already in StreamingAssets/video, played in the photograph's frame after the photograph
        /// (OpeningPresenter) and labelled "AI-generated illustration". Its provenance is its ASSETS.yaml row
        /// (tools/tests/test_opening_sources.py).</summary>
        public sealed class Clip
        {
            public string File { get; set; } = "";
            public string Shot { get; set; } = "";
            public string Alt { get; set; } = "";
            public bool AiGenerated { get; set; }
            public bool Sensitive { get; set; }
            public bool SensitivityReviewed { get; set; }
        }
        public class Paragraph
        {
            public string Text { get; set; } = "";
            public List<Source> Sources { get; set; } = new List<Source>();
            public bool Checked => !string.IsNullOrWhiteSpace(Text) && Sources != null && Sources.Count > 0 && Sources.All(s => s != null && s.IsChecked);
        }
        public sealed class Panel : Paragraph
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public List<Arrival> Arrivals { get; set; } = new List<Arrival>();
            public Image Image { get; set; } = new Image();
            public Clip? Clip { get; set; }
        }
        public sealed class Document
        {
            public string Status { get; set; } = "";
            public List<Panel> Panels { get; set; } = new List<Panel>();
            public Paragraph Closing { get; set; } = new Paragraph();
            public bool PhotosReviewed => Panels != null && Panels.Count == 3 && Panels.All(p => p != null && p.Image != null
                && p.Image.Sensitive && p.Image.SensitivityReviewed);
            public bool TextReviewed => Status == Statecraft.Sim.ReleaseGate.ReviewedStatus && Closing != null && Closing.Checked
                && Panels != null && Panels.Count == 3 && Panels.All(p => p != null && p.Checked);
            public bool Reviewed => PhotosReviewed && TextReviewed;
            /// <summary>Every sensitive clip has the owner's sensitivity review (a shareable build refuses one without it).</summary>
            public bool ClipsReviewed => Panels != null && Panels.All(p => p?.Clip == null || !p.Clip.Sensitive || p.Clip.SensitivityReviewed);
        }

        /// <summary>The approved three-panel structure; unknown or repeated YAML fields fail closed.</summary>
        public static Document Parse(string yaml)
        {
            try
            {
                var doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .WithDuplicateKeyChecking().Build().Deserialize<Document>(yaml ?? "");
                if (doc == null || doc.Panels == null || doc.Closing == null || doc.Closing.Sources == null
                    || string.IsNullOrWhiteSpace(doc.Closing.Text) || doc.Panels.Any(p => p == null || p.Image == null || p.Sources == null
                        || p.Arrivals == null || string.IsNullOrWhiteSpace(p.Text) || string.IsNullOrWhiteSpace(p.Title)))
                    throw new InvalidOperationException(SourcePath + ": missing paragraph or image");
                if (!doc.Panels.Select(p => p.Id).SequenceEqual(new[] { "europe", "arab_countries", "yemen" }))
                    throw new InvalidOperationException(SourcePath + ": expected Europe, Arab countries and Yemen in order");
                foreach (var paragraph in doc.Panels.Cast<Paragraph>().Concat(new[] { doc.Closing }))
                    if (paragraph.Sources.Count == 0 || paragraph.Sources.Any(s => s == null
                        || string.IsNullOrWhiteSpace(s.Url) || string.IsNullOrWhiteSpace(s.Supports)))
                        throw new InvalidOperationException(SourcePath + ": paragraph source or supported claim missing");
                foreach (var p in doc.Panels)
                    if (p.Image.File != "opening_" + p.Id + ".jpg" || string.IsNullOrWhiteSpace(p.Image.Credit)
                        || string.IsNullOrWhiteSpace(p.Image.Alt))
                        throw new InvalidOperationException(SourcePath + ": photograph or credit missing for " + p.Id);
                foreach (var p in doc.Panels.Where(p => p.Clip != null))
                    if (ClipProblem(p) is string problem) throw new InvalidOperationException(SourcePath + ": " + p.Id + "'s clip: " + problem);
                return doc;
            }
            catch (YamlDotNet.Core.YamlException e) { throw new InvalidOperationException(SourcePath + ": " + e.Message, e); }
        }

        /// <summary>Why a panel's clip row is malformed, or null: a bare .mp4 name in StreamingAssets/video, its shot, an alt
        /// text that says it is an illustration, ai_generated: true, sensitive like its photograph, and a review only of
        /// what is sensitive (the event-videos.yaml rules).</summary>
        static string? ClipProblem(Panel p)
        {
            var clip = p.Clip!;
            if (!System.Text.RegularExpressions.Regex.IsMatch(clip.File ?? "", "^[a-z0-9_-]+\\.mp4$"))
                return "file must be the bare name of an .mp4 in StreamingAssets/video (got '" + clip.File + "')";
            if (string.IsNullOrWhiteSpace(clip.Shot)) return "shot missing";
            if (clip.Alt == null || !clip.Alt.StartsWith("Illustration: ", StringComparison.Ordinal) || clip.Alt.Length < 30)
                return "alt text must start 'Illustration: ' and say what it shows";
            if (!clip.AiGenerated) return "ai_generated must be true";
            if (p.Image.Sensitive && !clip.Sensitive) return "the photograph is sensitive, so its clip must be sensitive: true";
            if (clip.SensitivityReviewed && !clip.Sensitive) return "sensitivity_reviewed without sensitive";
            return null;
        }

        public Document Read()
        {
            if (source == null) throw new InvalidOperationException(SourcePath + ": source is not assigned");
            var data = Parse(source.text);
            if (photos == null || photos.Length != data.Panels.Count || photos.Any(p => p == null))
                throw new InvalidOperationException(SourcePath + ": the three photograph references are not assigned");
            return data;
        }

        /// <summary>A fixture or an AI reading cannot approve the prose. Only Editor QA may preview a labelled draft.</summary>
        public static bool MayPresent(Document data, bool editorPreview) => data.PhotosReviewed && (data.TextReviewed || editorPreview);
    }
}
