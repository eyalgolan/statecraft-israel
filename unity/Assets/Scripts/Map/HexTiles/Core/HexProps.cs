#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Collection;

namespace Statecraft.HexTiles
{
    /// <summary>The hex tiles' streamed props: Tal Heuberger's 20 Higgsfield models (#1155), drawn by their "tiles:" ids.
    /// Owner, 23 October 2026: "Own streamed bundle". They are outside every Resources folder, so the first download does not
    /// carry them, and outside The 1948 Collection (<see cref="CollectionCatalog"/>, Tal's Tripo models only). They ship as
    /// one bundle of their own, <see cref="Bundle"/>, which the Collection's loader (<see cref="CollectionStream"/>) streams
    /// with this catalog after the first frame, and only when a hex board asks for one, so only with ?hex-tiles on. Until a
    /// prop arrives (and for good if its bundle fails) its placement draws the tile's stand-in. The editor (the painter, the
    /// captures, the tests) loads the prefabs from the AssetDatabase (HexModels.Prefab).
    /// Each prefab sits at Assets/Art/HexTiles/&lt;tile&gt;/&lt;prop&gt;.prefab, beside its model in
    /// Assets/Art/HexTiles/Props/&lt;tile&gt;_&lt;prop&gt;/ (HexSandboxImport.RunHiggsfield -prefabsRoot Assets/Art).</summary>
    public static class HexProps
    {
        public const string Bundle = "hex-props";
        /// <summary>The folder in the build's StreamingAssets that holds the bundle, and the checks file beside it.</summary>
        public const string StreamingFolder = "hex-props", ChecksFile = "hex-props-checks.txt";
        const string PrefabRoot = "Assets/Art/";

        /// <summary>The bundle's props, in a fixed order.</summary>
        public static readonly IReadOnlyList<string> Ids = new[]
        {
            "tiles:arab_village/minaret", "tiles:arab_village/prickly_pear",
            "tiles:caesarea_ruins/arches", "tiles:caesarea_ruins/boulders", "tiles:caesarea_ruins/jetty",
            "tiles:coastal_dunes/scrub",
            "tiles:desert_edge/tent",
            "tiles:jaffa_old_city/belfry", "tiles:jaffa_old_city/clock_tower", "tiles:jaffa_old_city/sea_minaret",
            "tiles:judean_highlands/garrigue",
            "tiles:railway_halt/crates", "tiles:railway_halt/eucalyptus", "tiles:railway_halt/packing_house",
            "tiles:railway_halt/station", "tiles:railway_halt/well",
            "tiles:village_mill/mill", "tiles:village_mill/sycamore_fig", "tiles:village_mill/terrace_wall",
            "tiles:village_mill/threshing_floor",
        };

        static readonly HashSet<string> ids = new HashSet<string>(Ids, StringComparer.Ordinal);

        public static bool Has(string id) => ids.Contains(id);

        /// <summary>The prop's prefab path in the project, which is also its name inside the bundle (lower case there).</summary>
        public static string PrefabPath(string id) => PrefabRoot + HexModels.PathOf(id) + ".prefab";

        /// <summary>What the loader streams: one bundle, one prefab per prop (the detail is not used).</summary>
        public static readonly CollectionStream.Catalog Catalog = new CollectionStream.Catalog("the hex-tile props", StreamingFolder,
            ChecksFile, id => Has(id) ? Bundle : null, (id, _) => PrefabPath(id));
    }
}
