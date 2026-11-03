#nullable enable
using System.IO;
using System.Linq;
using Statecraft.HexTiles;
using UnityEditor;

namespace Statecraft.Game.EditorTools
{
    /// <summary>The hex tiles' props bundles (<see cref="HexProps"/>; owner, 23 October 2026: "Own streamed bundle"): one
    /// bundle of the prop prefabs, and the papyrus alone in a second (the main map's Hula asks for it without the rest), whose
    /// models, materials and colour maps come in as dependencies, built by the
    /// Collection's bundle build into StreamingAssets/<see cref="HexProps.StreamingFolder"/> with its own checks file.
    /// Every Build.* player build runs it after The 1948 Collection's; HexPropsTests pins what it carries.</summary>
    public static class HexPropsBundle
    {
        /// <summary>Where a target's bundle is built (incremental, git-ignored).</summary>
        public static string Cache(BuildTarget target) => Path.Combine("Library", "HexPropBundles", target.ToString());

        public static AssetBundleBuild[] Builds() => new[]
        {
            new AssetBundleBuild { assetBundleName = HexProps.Bundle, assetNames = HexProps.Ids.Where(id => HexProps.BundleOf(id) == HexProps.Bundle).Select(HexProps.PrefabPath).ToArray() },
            new AssetBundleBuild { assetBundleName = HexProps.PapyrusBundle, assetNames = new[] { HexProps.PrefabPath(HexProps.Papyrus) } },
        };

        public static string BuildBundles(BuildTarget target) =>
            CollectionImport.BuildStreamed(target, Cache(target), Builds(), HexProps.StreamingFolder, HexProps.ChecksFile);
    }
}
