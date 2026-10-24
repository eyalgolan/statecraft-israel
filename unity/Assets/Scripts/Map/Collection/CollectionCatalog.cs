#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Game.Map.Collection
{
    /// <summary>Requested placement detail; both requests resolve to the same full original prefab. No far mesh is generated.</summary>
    public enum CollectionDetail { Far, Near }

    /// <summary>Tal Heuberger's 73 originals from #507 and #336, pinned by docs/original-models.json.
    /// Each has one CRC/SHA-checked bundle and one full-quality prefab outside Resources. Map requests wait
    /// for a drawable view; the shared loader waits until the first game frame has rendered (#1273).</summary>
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
        public static readonly IReadOnlyList<(string Bundle, string[] Ids)> Groups = new (string, string[])[]
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
        };

        // Category membership is for placement/import paths only; every original has its own bundle.
        public static IEnumerable<string> Ids => Groups.SelectMany(b => b.Ids).Concat(Kits.TownModels.Ids);
        public static readonly IReadOnlyList<(string Bundle, string[] Ids)> Bundles =
            Ids.Select(id => ("collection-" + id, new[] { id })).ToArray();
        public static bool IsBuilding(string id) => Kits.TownModels.Ids.Contains(id)
            || Groups.Single(b => b.Bundle == Buildings).Ids.Contains(id);

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
            IsBuilding(id)
                ? $"{BuildingsFolder}/{id}/{id}-stream.prefab"
                : $"{Folder}/{id}/{id}-stream.prefab";
    }
}
