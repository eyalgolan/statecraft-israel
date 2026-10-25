#nullable enable
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Statecraft.Game.UI
{
    /// <summary>Sharing a post with a screenshot of the game (#1170, SharePresenter): the capture, the PNG, the post text and
    /// the sites' share links. Nothing is uploaded by the game and nothing is counted: the picture stays in the page until the
    /// player shares or downloads it. On the web ShareShot.jslib keeps the PNG as a File; the Share press arms a one-shot
    /// release listener (as ExternalLink does, #1168), and inside that real gesture the browser's share sheet gets the file,
    /// the text and the itch URL (navigator.share), or, where it cannot share files (Firefox on the desktop, some others),
    /// the picture downloads instead and the panel offers the share links, which cannot carry an image.
    /// <para><see cref="Keep"/>, <see cref="CanShareFiles"/> and <see cref="Share"/> are what the panel calls; tests put
    /// recorders there (and put the defaults back), so nothing is saved or shared.</para></summary>
    public static class ShareShot
    {
        /// <summary>The game's itch page, the link the post carries (owner's decision, 20 Oct 2026).</summary>
        public const string Itch = "https://eyalgolan.itch.io/statecraft-israel";
        /// <summary>The game's GitHub repository, the post's second link (owner, 24 Oct 2026).</summary>
        public const string Repo = ExternalLink.Repository;
        /// <summary>The picture's long side at most: a DPR-3 phone frame encoded at full size can exhaust the WebGL heap.</summary>
        public const int MaxSide = 1920;
        /// <summary>The site buttons, in the panel's order: the owner's list (#1237; LinkedIn kept, at the end, owner 21 Oct
        /// 2026). Instagram has no share link: its button shares or downloads the picture, as Share does (SharePresenter).</summary>
        public static readonly string[] Sites = { "x", "facebook", "instagram", "whatsapp", "telegram", "linkedin" };

        public static Action<byte[], string> Keep = DefaultKeep;
        public static Func<bool> CanShareFiles = DefaultCanShareFiles;
        public static Action<string, string> Share = DefaultShare;
        /// <summary>True when the browser refused the share sheet (not a cancel) and the picture downloaded instead.</summary>
        public static Func<bool> Failed = DefaultFailed;
        /// <summary>What Copy (#1238) put on the clipboard: Pending until the browser has answered.</summary>
        public enum CopyResult { Pending, Picture, Text, Failed }
        /// <summary>Copies the kept picture on the player's release, or the post text if the browser refuses the picture.</summary>
        public static Action<string> Copy = DefaultCopy;
        public static Func<CopyResult> Copied = DefaultCopied;

        /// <summary>The post, in the UI's language, with the in-game date and turn, the itch URL and the repository's.</summary>
        public static string Text(UiText text, string date, int turn) => text.Format("share.post", date, turn, Itch, Repo);

        /// <summary>The post as the panel shows it (playtest, 25 Oct 2026). In the right-to-left Hebrew paragraph a URL broke
        /// across lines, "https://" ending one line and the rest starting the next, so in Hebrew each URL is kept whole: a word
        /// joiner (U+2060) after each '/', ':', '.' and '-' takes away the line breaks the text generator allows there.
        /// UiText.Format's marks already keep the URL a left-to-right run. (Rich text's nobr and a U+2066 isolate were tried
        /// and changed nothing under the advanced text generator.) English shows the post as it is. Only the label takes
        /// this: the text shared, posted and copied stays <see cref="Text"/>'s.</summary>
        public static string Shown(string post, bool hebrew) =>
            hebrew ? post.Replace(Itch, OneLine(Itch)).Replace(Repo, OneLine(Repo)) : post;

        public const char WordJoiner = (char)0x2060;

        static string OneLine(string url)
        {
            var line = new System.Text.StringBuilder(url.Length * 2);
            foreach (var c in url) { line.Append(c); if (c == '/' || c == ':' || c == '.' || c == '-') line.Append(WordJoiner); }
            return line.ToString();
        }

        /// <summary>A site's share link. X and WhatsApp take the text, which carries both URLs; Telegram takes the itch URL and
        /// the text, both encoded (core.telegram.org/widgets/share); Facebook and LinkedIn take only a URL, the itch one.</summary>
        public static string IntentUrl(string site, string text) => site switch
        {
            "x" => "https://x.com/intent/post?text=" + Uri.EscapeDataString(text),
            "facebook" => "https://www.facebook.com/sharer/sharer.php?u=" + Uri.EscapeDataString(Itch),
            "whatsapp" => "https://wa.me/?text=" + Uri.EscapeDataString(text),
            "telegram" => "https://t.me/share/url?url=" + Uri.EscapeDataString(Itch) + "&text=" + Uri.EscapeDataString(text),
            "linkedin" => "https://www.linkedin.com/sharing/share-offsite/?url=" + Uri.EscapeDataString(Itch),
            _ => throw new ArgumentException("no share link for " + site, nameof(site)),
        };

        /// <summary>The download's name: the in-game date the picture shows.</summary>
        public static string FileName(DateTime date) => "statecraft-" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".png";

        /// <summary>A frame's size scaled down to <see cref="MaxSide"/> on its long side (never up).</summary>
        public static Vector2Int Fit(int width, int height)
        {
            int longSide = Math.Max(width, height);
            if (longSide <= MaxSide) return new Vector2Int(width, height);
            double scale = (double)MaxSide / longSide;
            return new Vector2Int(Math.Max(1, (int)Math.Round(width * scale)), Math.Max(1, (int)Math.Round(height * scale)));
        }

        /// <summary>The frame as a PNG, scaled down on the GPU first when it is larger than the cap.</summary>
        public static byte[] Encode(Texture2D frame)
        {
            var size = Fit(frame.width, frame.height);
            if (size.x == frame.width && size.y == frame.height) return frame.EncodeToPNG();
            var rt = RenderTexture.GetTemporary(size.x, size.y, 0);
            var active = RenderTexture.active;
            var small = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            try
            {
                Graphics.Blit(frame, rt);
                RenderTexture.active = rt;
                small.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                small.Apply();
                return small.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(rt);
                if (Application.isPlaying) UnityEngine.Object.Destroy(small); else UnityEngine.Object.DestroyImmediate(small);   // edit-mode tests
            }
        }

        /// <summary>Captures the frame at its end, after the click that asked for it was handled (never inside it), as
        /// VisualShots does, and hands over its PNG. GameRoot runs it as a coroutine.</summary>
        public static IEnumerator Capture(Action<byte[]?> done)
        {
            yield return new WaitForEndOfFrame();
            byte[]? png = null;
            Texture2D? frame = null;
            // A failed capture is logged and hands over null: the panel stays shut and the Share buttons keep working.
            try { frame = ScreenCapture.CaptureScreenshotAsTexture(); png = Encode(frame); }
            catch (Exception e) { Debug.LogException(e); }
            finally { if (frame != null) UnityEngine.Object.Destroy(frame); }
            done(png);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void StatecraftShareKeep(byte[] png, int length, string name);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int StatecraftShareCanShareFiles();
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void StatecraftShareOnRelease(string text, string url);
        public static void DefaultKeep(byte[] png, string name) => StatecraftShareKeep(png, png.Length, name);
        public static bool DefaultCanShareFiles() => StatecraftShareCanShareFiles() == 1;
        public static void DefaultShare(string text, string url) => StatecraftShareOnRelease(text, url);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int StatecraftShareFailed();
        public static bool DefaultFailed() => StatecraftShareFailed() == 1;
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern void StatecraftShareCopyOnRelease(string text);
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int StatecraftShareCopied();
        public static void DefaultCopy(string text) => StatecraftShareCopyOnRelease(text);
        public static CopyResult DefaultCopied() => (CopyResult)StatecraftShareCopied();
#else
        static byte[]? kept; static string keptName = "";
        public static void DefaultKeep(byte[] png, string name) { kept = png; keptName = name; }
        /// <summary>The editor and desktop builds have no share sheet: the fallback, as in Firefox.</summary>
        public static bool DefaultCanShareFiles() => false;
        public static bool DefaultFailed() => false;
        /// <summary>The download, outside the browser: the picture goes into the temporary cache, and the log says where.</summary>
        public static void DefaultShare(string text, string url)
        {
            if (kept == null) return;
            var path = Path.Combine(Application.temporaryCachePath, keptName);
            File.WriteAllBytes(path, kept);
            Debug.Log("SHARE picture saved to " + path);
        }
        static CopyResult copied;
        /// <summary>Outside the browser Unity can only put text on the clipboard: the post.</summary>
        public static void DefaultCopy(string text) { GUIUtility.systemCopyBuffer = text; copied = CopyResult.Text; }
        public static CopyResult DefaultCopied() => copied;
#endif
    }
}
