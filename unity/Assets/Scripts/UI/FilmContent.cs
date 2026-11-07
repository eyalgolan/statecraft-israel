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
    /// <summary>Reads and checks Assets/Resources/Film/film.yaml, the opening film: a silent picture, a separate sound file
    /// and live caption cues (docs/event-videos.md, "The opening film"). The owner's open decisions are one value each at
    /// the top of that file. Presentation only: outside content/, so the content hash, the saves and the goldens never see
    /// it.</summary>
    public static class FilmContent
    {
        public const string ResourcePath = "Film/film";
        public const string SourcePath = "Assets/Resources/Film/film.yaml";
        public const string PictureFolder = "video", AudioFolder = "audio/film";
        /// <summary>The cue words are the strings film.cue.1, film.cue.2 and on, in both languages.</summary>
        public const string CueStem = "film";
        /// <summary>The label line's credit when the real recording plays: the terms ask for the creator and the archive (D-R).</summary>
        public const string RecordingCredit = "start.recording_credit";
        /// <summary>The repository's limit on one tracked file (scripts/oss/oss_policy.py, MAX_FILE_BYTES): a bundled picture
        /// or sound file stays under it unless the owner rules otherwise (film.yaml, picture_host).</summary>
        public const long BundledLimitBytes = 10 * 1024 * 1024;
        /// <summary>How long the picture may take to be ready: an 80 s file, so longer than a card clip's CardVideo.PrepareTimeoutMs
        /// (DESIGN VALUE).</summary>
        public const long PrepareTimeoutMs = 30000;

        public sealed class Cue
        {
            public double Start { get; set; } = -1;
            public double End { get; set; } = -1;
        }

        public sealed class Document
        {
            public bool AutoPlayOnNewGame { get; set; }
            public bool RealRecording { get; set; }
            public string AiLabel { get; set; } = "";
            public string PictureHost { get; set; } = "";
            public string Picture { get; set; } = "";
            public string Audio { get; set; } = "";
            public string AudioWithRecording { get; set; } = "";
            public bool AiGenerated { get; set; }
            public bool Sensitive { get; set; }
            public bool SensitivityReviewed { get; set; }
            public string HeReview { get; set; } = "";
            public List<Cue> Cues { get; set; } = new List<Cue>();
            /// <summary>The sound file that plays: real_recording (the owner's decision) picks it.</summary>
            public string SoundFile => RealRecording ? AudioWithRecording : Audio;
            public bool HebrewApproved => HeReview == OpeningContent.HeApproved;
        }

        /// <summary>Parses the file. Throws InvalidOperationException naming it on malformed YAML, an unknown key or a bad value.</summary>
        public static Document Parse(string yaml)
        {
            Document? doc;
            try
            {
                doc = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .WithDuplicateKeyChecking().Build().Deserialize<Document>(yaml ?? "");
            }
            catch (YamlDotNet.Core.YamlException e) { throw new InvalidOperationException(SourcePath + ": " + e.Message, e); }
            if (doc == null) throw new InvalidOperationException(SourcePath + ": empty");
            if (Problem(doc) is string problem) throw new InvalidOperationException(SourcePath + ": " + problem);
            return doc;
        }

        static string? Problem(Document d)
        {
            if (!Regex.IsMatch(d.Picture ?? "", "^[a-z0-9_-]+\\.mp4$")) return "picture must be a bare .mp4 name (got '" + d.Picture + "')";
            foreach (var audio in new[] { d.Audio, d.AudioWithRecording })
                if (!Regex.IsMatch(audio ?? "", "^[a-z0-9_-]+\\.ogg$")) return "audio must be a bare .ogg name (got '" + audio + "')";
            if (string.IsNullOrWhiteSpace(d.AiLabel)) return "ai_label must be a key of the strings files";
            if (!d.AiGenerated) return "ai_generated must be true";
            if (d.SensitivityReviewed && !d.Sensitive) return "sensitivity_reviewed without sensitive";
            if (d.HeReview != OpeningContent.HePending && d.HeReview != OpeningContent.HeApproved)
                return "he_review must be " + OpeningContent.HePending + " or " + OpeningContent.HeApproved;
            if (d.PictureHost == null || d.PictureHost.Length > 0
                && !(d.PictureHost.StartsWith("https://", StringComparison.Ordinal) && d.PictureHost.EndsWith("/", StringComparison.Ordinal)))
                return "picture_host must be empty or an https:// folder ending in /";
            if (d.Cues == null || d.Cues.Count == 0 || d.Cues.Any(c => c == null)) return "cues missing";
            for (int i = 0; i < d.Cues.Count; i++)
            {
                var cue = d.Cues[i];
                if (i == 0 && cue.Start < 0 || i > 0 && cue.Start < d.Cues[i - 1].End || cue.End <= cue.Start)
                    return "cue " + (i + 1) + ": starts must rise, each end after its start and by the next start";
            }
            return null;
        }

        /// <summary>The shipped file; null if it is not in the build, or if it is malformed (one error line): the opening then
        /// goes on without the film, and GameRoot.Begin does not stop.</summary>
        public static Document? Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) return null;
            try { return Parse(asset.text); }
            catch (InvalidOperationException e) { Debug.LogError("FILM " + e.Message + ": the opening goes on without it"); return null; }
        }

        /// <summary>A player build plays the film only once the owner has reviewed the final file (film.yaml,
        /// sensitivity_reviewed). The Editor may play it for QA, as it previews the opening's draft text.</summary>
        public static bool MayPlay(Document data, bool editorPreview) => data.SensitivityReviewed || editorPreview;

        /// <summary>The film's volume from the Settings rows (SettingsPresenter.SoundLevels): it holds voice and music, so it
        /// is the louder of Sound effects and Music, and silent only when both are off. The captions still run then: the
        /// muted sound keeps the clock.</summary>
        public static float Volume(float soundEffects, float music) => Mathf.Max(soundEffects, music);

        /// <summary>A film file's URL: an .mp4 from picture_host or StreamingAssets/video, a sound file from
        /// StreamingAssets/audio/film, with the build's id on the web (issue #437).</summary>
        public static string Url(Document data, string file) =>
            !file.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ? Streaming(AudioFolder + "/" + file)
            : data.PictureHost.Length > 0 ? data.PictureHost + file : Streaming(PictureFolder + "/" + file);

        static string Streaming(string path)
        {
            var url = Application.streamingAssetsPath + "/" + path;
            return url.Contains("://") ? Boot.GameInputs.WithBuildId(url, Application.buildGUID) : "file://" + url;
        }

        /// <summary>The film as AdvisorMedia plays it. The sound is the one English file in either UI, so it sits under "en";
        /// the caption language is the UiText the player was made with.</summary>
        public static AdvisorCatalog.Media Row(Document data) => new AdvisorCatalog.Media
        {
            Picture = data.Picture,
            Narration = new Dictionary<string, string> { ["en"] = data.SoundFile },
            Cues = new Dictionary<string, List<double>> { ["en"] = data.Cues.Select(c => c.Start).ToList() },
            CueEnds = data.Cues.Select(c => c.End).ToList(),
            Label = data.AiLabel,
        };
    }
}
