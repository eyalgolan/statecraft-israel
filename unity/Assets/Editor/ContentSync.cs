using System.IO;
using Statecraft.Game.Boot;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Copies the repository's content/ into Assets/StreamingAssets/content with a manifest.json and a bundle.json
    /// (every text in one file, which the web boot fetches: #1054) before every player build (git-ignored output), so
    /// players read the same content the editor reads from the repo.
    /// Run alone: Unity -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.ContentSync.Sync
    /// (copies as for a release player: the armistice lines' draft features left out).</summary>
    public sealed class ContentSync : IPreprocessBuildWithReport
    {
        public static string Target => Path.Combine(UnityEngine.Application.dataPath, "StreamingAssets", "content");
        public static string GoldenTarget => Path.Combine(UnityEngine.Application.dataPath, "StreamingAssets", "golden");

        public int callbackOrder => 0;

        /// <summary>A release player (no Development option) ships only the armistice lines the owner has signed off
        /// (ContentManifest.Copy; owner review of #2012); a development build ships them all, as it draws them all.</summary>
        public void OnPreprocessBuild(BuildReport report) => SyncTo((report.summary.options & BuildOptions.Development) == 0);

        // Not an overload of Sync: -executeMethod finds its method by name alone.
        public static void Sync() => SyncTo(release: true);

        static void SyncTo(bool release)
        {
            var files = ContentManifest.Copy(Paths.Content, Target, release);
            try { ContentManifest.Verify(Paths.Content, Target, release); }
            catch (InvalidDataException e) { throw new BuildFailedException("ContentSync: " + e.Message); }
            UnityEngine.Debug.Log($"ContentSync: {files.Count} files to {Target}");
            var source = Path.Combine(Paths.RepoRoot, "sim", "Statecraft.Sim.Tests");
            if (Directory.Exists(GoldenTarget)) Directory.Delete(GoldenTarget, true);
            Directory.CreateDirectory(Path.Combine(GoldenTarget, "scenarios"));
            File.Copy(Path.Combine(source, "golden", "manifest.txt"), Path.Combine(GoldenTarget, "manifest.txt"));
            foreach (var scenario in Directory.GetFiles(Path.Combine(source, "scenarios"), "*.yaml"))
                File.Copy(scenario, Path.Combine(GoldenTarget, "scenarios", Path.GetFileName(scenario)));
        }
    }
}
