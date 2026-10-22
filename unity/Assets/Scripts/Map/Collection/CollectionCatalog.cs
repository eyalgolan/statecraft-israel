#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Which model to draw: the strategic zoom's static mesh (at most 1,500 triangles; 4,500 for a vehicle, ship
    /// or aircraft) or the close zoom's (at most 8,000; skinned and playing its idle clip on the models that have one).</summary>
    public enum CollectionDetail { Far, Near }

    /// <summary>The 31 non-building models of The 1948 Collection, made by Tal Heuberger with Tripo Studio and
    /// offered in #507 (issue #664; the owner ruled them OK to use, 15 October 2026), and #507's nine whole-town models (issue
    /// #663; owner, 17 October 2026: "The nine whole-town models ship"), standing on their towns far off (<see cref="TownLandmarks"/>);
    /// and ten models offered only in #336, by Tal Heuberger: eight figures (issue #708; owner, 18 October 2026: "the 8 figures
    /// in #708 can be merged without his license line"), the pioneer farmer and a second Jerusalem town (issue #886; owner,
    /// 19 October 2026: "Yes, both"), placed by CivilianPeople, UnitFigures (the soldier) and TownLandmarks (the second
    /// Jerusalem, the city's only far-zoom model) (#708). Three independently generated 1948 infantry join the people bundle
    /// through their own rigging/export pipeline, with separate Syrian, ALA and Iraqi figure mappings. The Egyptian figure
    /// stays in review until its simultaneous bases have an approved map representation.
    /// Every model is placed but two, each by an owner ruling: b21, which
    /// waits for a sourced location (owner: wait), and #507's first Jerusalem, kept in the repository unused on the map
    /// (owner, 21 October 2026, #708).
    /// The original fifty #507/#336 models are slimmed by tools/slim_collection.py; the three new infantry are exported
    /// separately to the same runtime caps. All live in <see cref="Folder"/>/&lt;id&gt;/, outside every Resources folder. Each ships as two
    /// prefabs, &lt;id&gt;-far and &lt;id&gt;-near, in its category's AssetBundle, which the game streams after the first frame
    /// (<see cref="CollectionStream"/>), so the first download does not grow. A category is one bundle, so a placement
    /// downloads only its own kind. The buildings of #659 can join as one more bundle by adding a row here.</summary>
    public static class CollectionCatalog
    {
        public const string Folder = "Assets/Art/Collection";
        /// <summary>The folder in the build's StreamingAssets that holds the bundles.</summary>
        public const string StreamingFolder = "collection";

        /// <summary>The file beside the bundles, written by the bundle build, with one line per bundle:
        /// "&lt;bundle&gt; &lt;Unity CRC&gt; &lt;bytes&gt; &lt;SHA-256 of the file, hex&gt;". The loader opens only a bundle whose file
        /// matches its line, and passes the CRC to Unity, so a damaged bundle is never handed to Unity's reader.</summary>
        public const string ChecksFile = "collection-checks.txt";

        /// <summary>One bundle's line of <see cref="ChecksFile"/>.</summary>
        public readonly struct BundleCheck
        {
            public readonly uint Crc;
            public readonly long Bytes;
            public readonly string Sha256;
            public BundleCheck(uint crc, long bytes, string sha256) { Crc = crc; Bytes = bytes; Sha256 = sha256; }
            public override string ToString() => $"{Crc} {Bytes} {Sha256}";
        }

        /// <summary>Reads <see cref="ChecksFile"/>; null when any line is malformed (so a damaged list checks nothing).</summary>
        public static Dictionary<string, BundleCheck>? ParseChecks(string text)
        {
            var found = new Dictionary<string, BundleCheck>(StringComparer.Ordinal);
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var f = line.Split(' ');
                if (f.Length != 4 || !uint.TryParse(f[1], out var crc) || !long.TryParse(f[2], out var bytes) || f[3].Length != 64)
                    return null;
                found[f[0]] = new BundleCheck(crc, bytes, f[3]);
            }
            return found;
        }

        /// <summary>The SHA-256 of the file at <paramref name="path"/>, as lower-case hex.</summary>
        public static string Sha256(string path)
        {
            using var f = System.IO.File.OpenRead(path);
            using var sha = System.Security.Cryptography.SHA256.Create();
            return string.Concat(sha.ComputeHash(f).Select(x => x.ToString("x2")));
        }

        public const string People = "collection-people", Vehicles = "collection-vehicles", Animals = "collection-animals",
            Towns = "collection-towns", Buildings = "collection-special-buildings";

        /// <summary>Every bundle, with its models in a fixed order.</summary>
        public static readonly IReadOnlyList<(string Bundle, string[] Ids)> Bundles = new (string, string[])[]
        {
            (People, new[]
            {
                "figure-c01-arriving-man-v2", "figure-c02-townsman-v2", "figure-c03-smith-carpenter-v2", "figure-c04-sabra-pioneer-v3",
                "figure-c05-pioneer-veteran-v2", "figure-c06-young-recruit-v2", "figure-c07-veteran-fighter-v2",
                "figure-c08-arab-legion-v2", "figure-c09-irregular-v2", "figure-c10-woman-blue-dress-v2",
                "figure-c12f-palmach-scout-v2", "figure-c13-nurse-v2",
                // #336's eight figures (#708) and pioneer farmer (#886)
                "figure-arab-villager-v1", "figure-builder-1950-v1", "figure-elder-immigrant-v1", "figure-immigrant-child-v1",
                "figure-immigrant-woman-v1", "figure-kibbutz-woman-v1", "figure-soldier-1948-v1", "figure-yemenite-immigrant-v1",
                "figure-pioneer-farmer-v1-tpose",
                // Generated 1948 infantry: separate figures for the existing Syrian, ALA and Iraqi forces.
                "figure-syrian-infantry-1948-v1",
                "figure-ala-infantry-1948-v1", "figure-iraqi-infantry-1948-v1",
            }),
            (Vehicles, new[]
            {
                // road and rail (H01-H07, H09, H10)
                "figure-vehicle-willys-jeep-1948-v2", "figure-vehicle-sandwich-convoy-1948-v2", "figure-vehicle-napoleonchik-65mm-1948-v2",
                "figure-vehicle-intercity-bus-1948-v2", "figure-vehicle-horse-wagon-1948-v2", "figure-vehicle-farm-tractor-1948-v2",
                "figure-vehicle-citrus-truck-1948-v2", "figure-vehicle-canvas-lorry-1948-v2", "vehicle-steam-locomotive-1948-v2",
                // ships and aircraft (H11-H14)
                "vehicle-immigrant-steamer-1948-v2wl", "vehicle-fishing-boat-1948-v2wl", "vehicle-altalena-lst-1948-v2wl",
                "figure-vehicle-c54-skymaster-1948-v2",
            }),
            (Animals, new[]
            {
                "figure-donkey-1948-v1", "figure-draught-horse-idle-1948-v2", "figure-dromedary-camel-1948-v1",
                "figure-nubian-ibex-1948-v1", "figure-sheep-idle-1948-v1", "figure-striped-hyena-1948-v1",
            }),
            (Towns, new[]
            {
                // the nine whole-town models of #507, each one static mesh
                "tel-aviv-white-city-v1", "jerusalem-stone-city-v1", "haifa-port-city-v1", "moshava-town-v1", "nahalal-moshav-v1",
                "kibbutz-compound-v2", "stone-village-v1", "maabara-camp-v1", "shikun-housing-v1",
                // #336's Jerusalem variant (#886), an extra beside #507's Jerusalem
                "jerusalem-stone-city-v1-3denhance",
            }),
            (Buildings, new[]
            {
                "b11-shikun-housing-v2", "b12-tegart-fort-v2", "b18-military-barracks-v2",
                "b21-artisan-workshop-v2", "b22-industrial-factory-v2",
            }),
        };

        /// <summary>The models whose near prefab is skinned and plays its idle clip: every person but C05 (his only idle is
        /// a walk; #507 has him stand still) and #336's soldier (no rig), the sheep and the draught horse.</summary>
        public static readonly IReadOnlyCollection<string> Animated = new HashSet<string>(StringComparer.Ordinal)
        {
            "figure-c01-arriving-man-v2", "figure-c02-townsman-v2", "figure-c03-smith-carpenter-v2", "figure-c04-sabra-pioneer-v3",
            "figure-c06-young-recruit-v2", "figure-c07-veteran-fighter-v2", "figure-c08-arab-legion-v2", "figure-c09-irregular-v2",
            "figure-c10-woman-blue-dress-v2", "figure-c12f-palmach-scout-v2", "figure-c13-nurse-v2",
            "figure-draught-horse-idle-1948-v2", "figure-sheep-idle-1948-v1",
            "figure-arab-villager-v1", "figure-builder-1950-v1", "figure-elder-immigrant-v1", "figure-immigrant-child-v1",
            "figure-immigrant-woman-v1", "figure-kibbutz-woman-v1", "figure-yemenite-immigrant-v1", "figure-pioneer-farmer-v1-tpose",
            "figure-syrian-infantry-1948-v1",
            "figure-ala-infantry-1948-v1", "figure-iraqi-infantry-1948-v1",
        };

        public static IEnumerable<string> Ids => Bundles.SelectMany(b => b.Ids);

        /// <summary>The bundle that carries <paramref name="id"/>, or null for an id the collection does not have.</summary>
        public static string? BundleOf(string id)
        {
            foreach (var (bundle, ids) in Bundles)
                if (Array.IndexOf(ids, id) >= 0) return bundle;
            return null;
        }

        /// <summary>Where #675 imported Tal Heuberger's single-building models; the special-buildings bundle's prefabs (#707) sit
        /// beside each model there, one prefab for both details (the buildings draw only close up).</summary>
        public const string BuildingsFolder = "Assets/Art/Settlements/Prototype/Models";

        /// <summary>The prefab's project path, which is also its name inside the bundle (lower case there).</summary>
        public static string PrefabPath(string id, CollectionDetail detail) =>
            BundleOf(id) == Buildings
                ? $"{BuildingsFolder}/{id}/{id}-stream.prefab"
                : $"{Folder}/{id}/{id}-{(detail == CollectionDetail.Far ? "far" : "near")}.prefab";
    }
}
