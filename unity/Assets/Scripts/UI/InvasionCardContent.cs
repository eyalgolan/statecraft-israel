#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Statecraft.Game.UI
{
    /// <summary>The Arab armies' invasion card on New game (Resources/InvasionCard/invasion-card.yaml; #3142, owner, 11 Nov
    /// 2026: "One card, step per front (Recommended)", "Info only, turn 1 only (Recommended)"): an intro, then one step per
    /// front, each naming the units_1948.yaml row whose token the camera centres on. Presentation only, like the raid card:
    /// not game content, no sim event, no number, no save. A strict reader: unknown or repeated fields, a source without its
    /// claim, a front step without its row and side, or Hebrew that is not complete fail closed. The sim test
    /// InvasionCardTests holds each step's row to the forces the game starts with.</summary>
    public static class InvasionCardContent
    {
        public const string ResourcePath = "InvasionCard/invasion-card";
        public const string SourcePath = "Assets/Resources/InvasionCard/invasion-card.yaml";

        public sealed class Step
        {
            /// <summary>The units_1948.yaml row whose token the camera centres on; null on the intro.</summary>
            public string? Focus { get; set; }
            /// <summary>That row's side (the sim test checks it against the row).</summary>
            public string? Side { get; set; }
            public string Text { get; set; } = "";
            public string? TextHe { get; set; }
        }

        public sealed class Document
        {
            public string Status { get; set; } = "";
            public string Title { get; set; } = "";
            public string? TitleHe { get; set; }
            public string? HeReview { get; set; }
            public List<Step> Steps { get; set; } = new List<Step>();
            public List<OpeningContent.Source> Sources { get; set; } = new List<OpeningContent.Source>();
            public bool HasHebrew => !string.IsNullOrWhiteSpace(TitleHe) && !string.IsNullOrWhiteSpace(HeReview)
                && Steps.All(s => !string.IsNullOrWhiteSpace(s.TextHe));
            public bool HebrewApproved => HasHebrew && HeReview == OpeningContent.HeApproved;
            /// <summary>What a player build needs: the owner's reviewed status (as the opening and the raid card), every
            /// source checked by a person, and the Hebrew read by a native speaker who did not write it.</summary>
            public bool Reviewed => Status == Statecraft.Sim.ReleaseGate.ReviewedStatus && Sources.Count > 0
                && Sources.All(s => s.IsChecked) && HebrewApproved;
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
            if (doc == null || string.IsNullOrWhiteSpace(doc.Title) || doc.Steps == null || doc.Steps.Count < 2
                || doc.Steps.Any(s => s == null || string.IsNullOrWhiteSpace(s.Text)))
                throw new InvalidOperationException(SourcePath + ": a title and at least two steps, each with its text, are required");
            if (doc.Steps[0].Focus != null || doc.Steps[0].Side != null)
                throw new InvalidOperationException(SourcePath + ": the first step is the intro, with no focus or side");
            if (doc.Steps.Skip(1).Any(s => string.IsNullOrWhiteSpace(s.Focus) || string.IsNullOrWhiteSpace(s.Side)))
                throw new InvalidOperationException(SourcePath + ": every step after the intro names its focus row and side");
            if (doc.Sources == null || doc.Sources.Count == 0 || doc.Sources.Any(s => s == null
                || string.IsNullOrWhiteSpace(s.Url) || string.IsNullOrWhiteSpace(s.Supports)))
                throw new InvalidOperationException(SourcePath + ": every fact needs a source with its url and what it supports");
            bool anyHebrew = doc.TitleHe != null || doc.HeReview != null || doc.Steps.Any(s => s.TextHe != null);
            if (anyHebrew && !doc.HasHebrew)
                throw new InvalidOperationException(SourcePath + ": the Hebrew comes complete or not at all (title_he, he_review, every step's text_he)");
            if (doc.HeReview != null && doc.HeReview != OpeningContent.HePending && doc.HeReview != OpeningContent.HeApproved)
                throw new InvalidOperationException(SourcePath + ": he_review must be " + OpeningContent.HePending + " or "
                    + OpeningContent.HeApproved);
            return doc;
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
