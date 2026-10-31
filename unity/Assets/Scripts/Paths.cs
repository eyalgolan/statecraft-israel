using System.IO;
using UnityEngine;

namespace Statecraft.Game
{
    public static class Paths
    {
        // In the editor, content is read from the repo. Copying content into player builds comes in M3 (first player build).
        public static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        public static string Content => Application.isEditor ? Path.Combine(RepoRoot, "content") : Path.Combine(Application.streamingAssetsPath, "content");
        public static string StreamingMap => Path.Combine(Application.streamingAssetsPath, "map");
        /// <summary>translations/ beside Content (#1618): the repo's in the editor, StreamingAssets' copy in a player.</summary>
        public static string Translations => Path.Combine(Path.GetDirectoryName(Content)!, "translations");
    }
}
