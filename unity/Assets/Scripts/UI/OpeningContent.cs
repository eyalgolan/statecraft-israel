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
        /// <summary>he_review: the Hebrew is an agent draft until a native speaker who did not write it has read it.</summary>
        public const string HePending = "pending", HeApproved = "approved";

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
            public string? CountryHe { get; set; }
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
        public class Paragraph
        {
            public string Text { get; set; } = "";
            public string? TextHe { get; set; }
            public string? HeReview { get; set; }
            public List<Source> Sources { get; set; } = new List<Source>();
            public bool Checked => !string.IsNullOrWhiteSpace(Text) && Sources != null && Sources.Count > 0 && Sources.All(s => s != null && s.IsChecked);
            /// <summary>Its Hebrew fields, null where absent: all of them or none (Parse).</summary>
            public virtual IEnumerable<string?> HebrewFields => new[] { TextHe, HeReview };
            public bool HasHebrew => HebrewFields.All(f => !string.IsNullOrWhiteSpace(f));
            public bool HebrewApproved => HasHebrew && HeReview == HeApproved;
        }
        public sealed class Panel : Paragraph
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public string? TitleHe { get; set; }
            public List<Arrival> Arrivals { get; set; } = new List<Arrival>();
            public Image Image { get; set; } = new Image();
            public override IEnumerable<string?> HebrewFields =>
                base.HebrewFields.Append(TitleHe).Concat((Arrivals ?? new List<Arrival>()).Select(a => a?.CountryHe));
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
            /// <summary>Every panel and the closing carry their Hebrew, read by a native speaker (owner, 1 Nov 2026, "Translate
            /// to Hebrew (Recommended)"): the English rule's counterpart, so a player never sees an unread draft.</summary>
            public bool HebrewReviewed => Closing != null && Closing.HebrewApproved
                && Panels != null && Panels.Count == 3 && Panels.All(p => p != null && p.HebrewApproved);
            public bool Reviewed => PhotosReviewed && TextReviewed && HebrewReviewed;
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
                foreach (var (name, paragraph) in doc.Panels.Select(p => (p.Id, (Paragraph)p)).Append(("closing", doc.Closing)))
                {
                    if (paragraph.HebrewFields.Any(f => f != null) && !paragraph.HasHebrew)
                        throw new InvalidOperationException(SourcePath + ": " + name + ": the Hebrew comes complete or not at all"
                            + (name == "closing" ? " (text_he, he_review)" : " (title_he, text_he, he_review and each arrival's country_he)"));
                    if (paragraph.HeReview != null && paragraph.HeReview != HePending && paragraph.HeReview != HeApproved)
                        throw new InvalidOperationException(SourcePath + ": " + name + ": the Hebrew's he_review must be "
                            + HePending + " or " + HeApproved);
                }
                foreach (var p in doc.Panels)
                    if (p.Image.File != "opening_" + p.Id + ".jpg" || string.IsNullOrWhiteSpace(p.Image.Credit)
                        || string.IsNullOrWhiteSpace(p.Image.Alt))
                        throw new InvalidOperationException(SourcePath + ": photograph or credit missing for " + p.Id);
                return doc;
            }
            catch (YamlDotNet.Core.YamlException e) { throw new InvalidOperationException(SourcePath + ": " + e.Message, e); }
        }

        public Document Read()
        {
            if (source == null) throw new InvalidOperationException(SourcePath + ": source is not assigned");
            var data = Parse(source.text);
            if (photos == null || photos.Length != data.Panels.Count || photos.Any(p => p == null))
                throw new InvalidOperationException(SourcePath + ": the three photograph references are not assigned");
            return data;
        }

        /// <summary>A fixture or an AI reading cannot approve the prose, English or Hebrew. Only Editor QA may preview a
        /// labelled draft.</summary>
        public static bool MayPresent(Document data, bool editorPreview) =>
            data.PhotosReviewed && (data.TextReviewed && data.HebrewReviewed || editorPreview);
    }
}
