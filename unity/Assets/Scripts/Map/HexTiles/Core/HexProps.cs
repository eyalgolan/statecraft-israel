#nullable enable
using System;
using System.Collections.Generic;
using Statecraft.Game.Map.Collection;

namespace Statecraft.HexTiles
{
    /// <summary>The hex tiles' streamed props: Tal Heuberger's 20 Higgsfield models (#1155) and their papyrus and tamarisk
    /// (#2082; owner, 2 November 2026: "Hula + Arava, map and boards (Recommended)"), drawn by their "tiles:" ids.
    /// Owner, 23 October 2026: "Own streamed bundle". They are outside every Resources folder, so the first download does not
    /// carry them, and outside The 1948 Collection (<see cref="CollectionCatalog"/>, Tal's Tripo models only). They ship as
    /// one bundle of their own, <see cref="Bundle"/>, which the Collection's loader (<see cref="CollectionStream"/>) streams
    /// with this catalog after the first frame, and only when a hex board asks for one (so with ?hex-tiles on) or when the
    /// main map's first camera does (<see cref="Statecraft.Game.Map.Collection.HexPropModels"/>, after the first frame). Until a prop arrives
    /// (and for good if its bundle fails) a board's placement draws the tile's stand-in. The editor (the painter, the
    /// captures, the tests) loads the prefabs from the AssetDatabase (HexModels.Prefab).
    /// Each prefab sits at Assets/Art/HexTiles/&lt;tile&gt;/&lt;prop&gt;.prefab, beside its model in
    /// Assets/Art/HexTiles/Props/&lt;tile&gt;_&lt;prop&gt;/ (HexSandboxImport.RunHiggsfield -prefabsRoot Assets/Art).
    /// The papyrus is a bundle of its own, <see cref="PapyrusBundle"/>, in the same stream: the main map asks for it at the
    /// Hula (HulaPapyrusLayer) without the hex boards' other props.</summary>
    public static class HexProps
    {
        public const string Bundle = "hex-props", PapyrusBundle = "hex-props-papyrus";
        public const string Papyrus = "tiles:hula_swamp/papyrus", Tamarisk = "tiles:arava_oasis/tamarisk";
        /// <summary>The folder in the build's StreamingAssets that holds the bundle, and the checks file beside it.</summary>
        public const string StreamingFolder = "hex-props", ChecksFile = "hex-props-checks.txt";
        const string PrefabRoot = "Assets/Art/";

        /// <summary>The bundle's props, in a fixed order.</summary>
        public static readonly IReadOnlyList<string> Ids = new[]
        {
            "tiles:arab_village/minaret", "tiles:arab_village/prickly_pear",
            Tamarisk,
            "tiles:caesarea_ruins/arches", "tiles:caesarea_ruins/boulders", "tiles:caesarea_ruins/jetty",
            "tiles:coastal_dunes/scrub",
            "tiles:desert_edge/tent",
            Papyrus,
            "tiles:jaffa_old_city/belfry", "tiles:jaffa_old_city/clock_tower", "tiles:jaffa_old_city/sea_minaret",
            "tiles:judean_highlands/garrigue",
            "tiles:railway_halt/crates", "tiles:railway_halt/eucalyptus", "tiles:railway_halt/packing_house",
            "tiles:railway_halt/station", "tiles:railway_halt/well",
            "tiles:village_mill/mill", "tiles:village_mill/sycamore_fig", "tiles:village_mill/terrace_wall",
            "tiles:village_mill/threshing_floor",
        };

        static readonly HashSet<string> ids = new HashSet<string>(Ids, StringComparer.Ordinal);

        public static bool Has(string id) => ids.Contains(id);

        /// <summary>The prop's bundle: <see cref="PapyrusBundle"/> for the papyrus, <see cref="Bundle"/> for the rest; null
        /// for an id that is not a prop.</summary>
        public static string? BundleOf(string id) => !Has(id) ? null : id == Papyrus ? PapyrusBundle : Bundle;

        /// <summary>The prop's prefab path in the project, which is also its name inside the bundle (lower case there).</summary>
        public static string PrefabPath(string id) => PrefabRoot + HexModels.PathOf(id) + ".prefab";

        /// <summary>What the loader streams: two bundles, one prefab per prop (the detail is not used).</summary>
        public static readonly CollectionStream.Catalog Catalog = new CollectionStream.Catalog("the hex-tile props", StreamingFolder,
            ChecksFile, BundleOf, (id, _) => PrefabPath(id));
    }
}
