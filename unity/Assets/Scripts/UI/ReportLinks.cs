#nullable enable
using System;
using System.Linq;
using UnityEngine;

namespace Statecraft.Game.UI
{
    /// <summary>The links the "Report a bug or idea" panel opens (#1169, FeedbackPresenter). Both issue templates are YAML
    /// forms, which GitHub prefills by field id from the query and where it ignores `body=` and `labels=`: the bug form
    /// gets its title, `version` and `env`, and leaves `what` and `repro` for the player; the feature form gets its title
    /// only. Nothing personal goes in: the version is the app's version and the content hash, the platform the browser's
    /// user agent and screen size, and the panel shows every value before anything opens. Each value is clipped to
    /// <see cref="MaxValue"/> characters, so the whole URL stays under GitHub's <see cref="MaxUrl"/>.</summary>
    public static class ReportLinks
    {
        public const string BugTitle = "Bug: ", FeatureTitle = "Idea: ";
        /// <summary>The game's itch page; its comments are at the bottom, for a player with no GitHub account.</summary>
        public const string Itch = "https://eyalgolan.itch.io/statecraft-israel";
        /// <summary>GitHub refuses a longer issue URL.</summary>
        public const int MaxUrl = 8000;
        /// <summary>A character encodes to at most 9 (three %XX), so two such values and the rest stay under MaxUrl.</summary>
        public const int MaxValue = 400;

        /// <summary>The version line: GameBoot logs the same hash as `BOOT OK content ...`. The build carries no commit SHA.</summary>
        public static string Version(string appVersion, string contentHash) => $"{appVersion} / content {contentHash}";

        /// <summary>The bug form's prefilled fields, as the panel shows them and the URL carries them.</summary>
        public static (string id, string value)[] BugFields(string version, string env) =>
            new[] { ("title", BugTitle), ("version", Clip(version)), ("env", Clip(env)) };

        public static (string id, string value)[] FeatureFields() => new[] { ("title", FeatureTitle) };

        public static string Bug(string version, string env) => Url("bug_report.yml", BugFields(version, env));

        public static string Feature() => Url("feature_request.yml", FeatureFields());

        static string Url(string template, (string id, string value)[] fields) =>
            ExternalLink.Repository + "/issues/new?template=" + template
            + string.Concat(fields.Select(f => "&" + f.id + "=" + Uri.EscapeDataString(f.value)));

        static string Clip(string value)
        {
            if (value.Length <= MaxValue) return value;
            int n = char.IsHighSurrogate(value[MaxValue - 1]) ? MaxValue - 1 : MaxValue;   // never half a pair
            return value.Substring(0, n);
        }

        /// <summary>The platform line: in the browser its user agent and screen size (ExternalLink.jslib); elsewhere the
        /// operating system, the Unity version and the screen size.</summary>
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern string StatecraftBrowserEnv();
        public static string Environment() => StatecraftBrowserEnv() ?? "";
#else
        public static string Environment() =>
            $"{SystemInfo.operatingSystem} / Unity {Application.unityVersion}, screen {Screen.width}x{Screen.height}";
#endif
    }
}
