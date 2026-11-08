#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.UI
{
    /// <summary>The Tel Aviv raid card's text, sources and clip (Resources/RaidIntro/raid-intro.yaml; owner, 8 Nov 2026,
    /// "15 May dawn raid (Recommended)"). Presentation only, like the opening (#530): not game content, no sim event, no
    /// number, no save. A strict reader: unknown or repeated fields, a source without its claim, a clip not labelled as an
    /// AI illustration or Hebrew that is not complete fail closed.</summary>
    public static class RaidCardContent
    {
        public const string ResourcePath = "RaidIntro/raid-intro";
        public const string SourcePath = "Assets/Resources/RaidIntro/raid-intro.yaml";

        public sealed class Clip
        {
            public string File { get; set; } = "";
            public string Alt { get; set; } = "";
            public string AltHe { get; set; } = "";
            public string AltHeReview { get; set; } = "";
            public string Credit { get; set; } = "";
            public bool AiGenerated { get; set; }
            public bool Sensitive { get; set; }
            public bool SensitivityReviewed { get; set; }
        }

        public sealed class Document
        {
            public string Status { get; set; } = "";
            public string Title { get; set; } = "";
            public string? TitleHe { get; set; }
            public string Text { get; set; } = "";
            public string? TextHe { get; set; }
            public string? HeReview { get; set; }
            public List<OpeningContent.Source> Sources { get; set; } = new List<OpeningContent.Source>();
            public Clip Clip { get; set; } = new Clip();
            public bool HasHebrew => !string.IsNullOrWhiteSpace(TitleHe) && !string.IsNullOrWhiteSpace(TextHe)
                && !string.IsNullOrWhiteSpace(HeReview);
            public bool HebrewApproved => HasHebrew && HeReview == OpeningContent.HeApproved;
            /// <summary>What a player build needs: the reviewed status, every source checked, the Hebrew read by a native
            /// speaker who did not write it, and the owner's sensitivity review of the clip.</summary>
            public bool Reviewed => Status == Statecraft.Sim.ReleaseGate.ReviewedStatus && Sources.Count > 0
                && Sources.All(s => s.IsChecked) && HebrewApproved && Clip.SensitivityReviewed;
        }

        public static Document Parse(string yaml)
        {
            Document? doc;
            try
            {
                doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .WithDuplicateKeyChecking().Build().Deserialize<Document>(yaml ?? "");
            }
            catch (YamlDotNet.Core.YamlException e) { throw new InvalidOperationException(SourcePath + ": " + e.Message, e); }
            if (doc == null || string.IsNullOrWhiteSpace(doc.Title) || string.IsNullOrWhiteSpace(doc.Text) || doc.Clip == null)
                throw new InvalidOperationException(SourcePath + ": title, text and clip are required");
            if (doc.Sources == null || doc.Sources.Count == 0 || doc.Sources.Any(s => s == null
                || string.IsNullOrWhiteSpace(s.Url) || string.IsNullOrWhiteSpace(s.Supports)))
                throw new InvalidOperationException(SourcePath + ": every fact needs a source with its url and what it supports");
            bool anyHebrew = doc.TitleHe != null || doc.TextHe != null || doc.HeReview != null;
            if (anyHebrew && !doc.HasHebrew)
                throw new InvalidOperationException(SourcePath + ": the Hebrew comes complete or not at all (title_he, text_he, he_review)");
            if (doc.HeReview != null && doc.HeReview != OpeningContent.HePending && doc.HeReview != OpeningContent.HeApproved)
                throw new InvalidOperationException(SourcePath + ": he_review must be " + OpeningContent.HePending + " or "
                    + OpeningContent.HeApproved);
            if (ClipProblem(doc.Clip) is string problem) throw new InvalidOperationException(SourcePath + ": clip: " + problem);
            return doc;
        }

        /// <summary>Why the clip row is malformed, or null (the event-videos.yaml and opening.yaml clip rules): a bare .mp4
        /// name in StreamingAssets/video, an alt text that says it is an illustration, Hebrew alt text marked agent-checked
        /// where present, the photographs' credit line, ai_generated, and sensitive (the aftermath of a raid that killed
        /// people).</summary>
        static string? ClipProblem(Clip clip)
        {
            if (!Regex.IsMatch(clip.File ?? "", "^[a-z0-9_-]+\\.mp4$"))
                return "file must be the bare name of an .mp4 in StreamingAssets/video (got '" + clip.File + "')";
            if (clip.Alt == null || !clip.Alt.StartsWith("Illustration: ", StringComparison.Ordinal) || clip.Alt.Length < 30)
                return "alt text must start 'Illustration: ' and say what it shows";
            if (!string.IsNullOrWhiteSpace(clip.AltHeReview) && (string.IsNullOrWhiteSpace(clip.AltHe) || clip.AltHeReview != "agent-checked"))
                return "alt_he_review must be agent-checked, with alt_he";
            if (string.IsNullOrWhiteSpace(clip.Credit)) return "the photographs' credit line is missing";
            if (!clip.AiGenerated) return "ai_generated must be true";
            if (!clip.Sensitive) return "sensitive must be true";
            return null;
        }

        /// <summary>The shipped card, or null when the file is missing.</summary>
        public static Document? Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            return asset == null ? null : Parse(asset.text);
        }

        /// <summary>A fixture or an AI reading cannot approve it: only Editor QA may preview the labelled draft.</summary>
        public static bool MayPresent(Document data, bool editorPreview) => data.Reviewed || editorPreview;
    }
}
