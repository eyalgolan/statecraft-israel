#nullable enable
using System.IO;
using System.Linq;
using Statecraft.HexTiles;
using UnityEditor;

namespace Statecraft.Game.EditorTools
{
    /// <summary>The hex tiles' props bundle (<see cref="HexProps"/>; owner, 23 October 2026: "Own streamed bundle"): one
    /// bundle of the 20 prop prefabs, whose models, materials and colour maps come in as dependencies, built by the
    /// Collection's bundle build into StreamingAssets/<see cref="HexProps.StreamingFolder"/> with its own checks file.
    /// Every Build.* player build runs it after The 1948 Collection's; HexPropsTests pins what it carries.</summary>
    public static class HexPropsBundle
    {
        /// <summary>Where a target's bundle is built (incremental, git-ignored).</summary>
        public static string Cache(BuildTarget target) => Path.Combine("Library", "HexPropBundles", target.ToString());

        public static AssetBundleBuild[] Builds() => new[]
        {
            new AssetBundleBuild { assetBundleName = HexProps.Bundle, assetNames = HexProps.Ids.Select(HexProps.PrefabPath).ToArray() },
        };

        public static string BuildBundles(BuildTarget target) =>
            CollectionImport.BuildStreamed(target, Cache(target), Builds(), HexProps.StreamingFolder, HexProps.ChecksFile);
    }
}
