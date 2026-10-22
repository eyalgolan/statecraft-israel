#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Statecraft.HexTiles;
using UnityEngine;

/// <summary>The regional farm and forest variants (two per type and region, docs/hex-tiles/README.md): every type has
/// an a and a b for each region, tagged with its kind and region; a and b build differently from each other and from
/// their base; and the base tiles the existing boards use still build exactly what they built before the variants
/// subclassed them (a fingerprint of every placement and paint, recorded before the refactor).</summary>
public class HexVariantTests
{
    static readonly (string type, string kind)[] Types =
    {
        ("wheat_fields", "farm"), ("citrus_orchard", "farm"), ("olive_terraces", "farm"), ("mixed_farm", "farm"), ("forest", "forest"),
    };

    static IEnumerable<string> VariantIds() =>
        from t in Types from r in HexPrototypes.Regions from v in new[] { "a", "b" } select $"{t.type}_{r}_{v}";

    static HexTileContext Built(string id, int q = 2, int r = -1, int seed = 1948)
    {
        var ctx = new HexTileContext(q, r, new FlatSurface(), seed);
        HexPrototypes.Create(id).Build(ctx);
        return ctx;
    }

    static string F(float f) => f.ToString("R", CultureInfo.InvariantCulture);
    static string V(Vector3 v) => $"{F(v.x)},{F(v.y)},{F(v.z)}";

    /// <summary>Every placement and paint of a build, as text, hashed (FNV-1a 64) so the expected value is one line.</summary>
    static string Fingerprint(HexTileContext ctx)
    {
        var sb = new StringBuilder();
        foreach (var p in ctx.Placements)
        {
            sb.Append(p.Model).Append('|').Append(p.StandIn).Append('|').Append(p.Fit).Append('|').Append(F(p.Tint.r)).Append(',').Append(F(p.Tint.g)).Append(',').Append(F(p.Tint.b));
            for (int i = 0; i < 16; i++) sb.Append('|').Append(F(p.Matrix[i]));
            sb.Append('\n');
        }
        foreach (var p in ctx.Paints)
            sb.Append(p.Layer).Append('|').Append(V(p.A)).Append('|').Append(V(p.B)).Append('|').Append(F(p.Width)).Append('|').Append(F(p.Depth))
                .Append('|').Append(F(p.Yaw)).Append('|').Append(F(p.Soft)).Append('|').Append(F(p.Strength)).Append('|').Append(p.Line).Append('\n');
        ulong h = 14695981039346656037UL;
        foreach (char c in sb.ToString()) { h ^= c; h *= 1099511628211UL; }
        return $"{ctx.Placements.Count}/{ctx.Paints.Count}/{h:x16}";
    }

    static string Fingerprints(string id) =>
        string.Join(" ", new[] { Fingerprint(Built(id)), Fingerprint(Built(id, -3, 5, 7)), Fingerprint(Built(id, 11, -8, 31415)) });

    [TestCaseSource(nameof(VariantIds))]
    public void IsRegisteredWithItsKindAndRegion(string id)
    {
        Assert.That(HexPrototypes.All.ContainsKey(id), $"no prototype '{id}'");
        var info = HexPrototypes.Info(id);
        string kind = Types.First(t => id.StartsWith(t.type + "_")).kind, region = id.Split('_')[id.Split('_').Length - 2];
        Assert.That((info.Kind, info.Region), Is.EqualTo((kind, region)), id);
    }

    [TestCaseSource(nameof(VariantIds))]
    public void BuildsDifferentlyFromItsPairAndItsBase(string id)
    {
        string pair = id.EndsWith("_a") ? id.Substring(0, id.Length - 1) + "b" : id.Substring(0, id.Length - 1) + "a";
        string type = Types.First(t => id.StartsWith(t.type + "_")).type, baseId = type == "forest" ? "pine_forest" : type;
        Assert.That(Fingerprints(id), Is.Not.EqualTo(Fingerprints(pair)), $"{id} builds as {pair}");
        Assert.That(Fingerprints(id), Is.Not.EqualTo(Fingerprints(baseId)), $"{id} builds as {baseId}");
    }

    [Test]
    public void MixedFarmIsAFarmForAnyRegion()
    {
        Assert.That(HexPrototypes.All.ContainsKey("mixed_farm"), "no prototype 'mixed_farm'");
        var info = HexPrototypes.Info("mixed_farm");
        Assert.That((info.Kind, info.Region), Is.EqualTo(("farm", "")));
    }

    /// <summary>Recorded on 4841e34c, before the bases were opened for subclassing (FlatSurface, three hexes and seeds), then
    /// re-recorded on a clean checkout after main's #1160 sized a missing model's footprint by its stand-in: the same tiles
    /// on the same code, but without the git-ignored local models (Resources/HexSandbox, Resources/SettlementPrototypes)
    /// some trees no longer fit and the rest shift. With those models present #1160 changes nothing, so the test is ignored
    /// there rather than pinned twice.</summary>
    static readonly Dictionary<string, string> Before = new Dictionary<string, string>
    {
        ["wheat_fields"] = "6/23/8dc0de48cc6e25f0 11/21/0ec106c62c81994e 8/23/0b8b66bfd537c34d",
        ["grazing_land"] = "22/0/1389752806d94528 22/0/312ddbb3232f3c2b 22/0/fece2adb178546f6",
        ["citrus_orchard"] = "27/6/3fc025f769e42c67 27/6/1ec6cc42eb7d765f 27/6/c992d3cc18de5d6c",
        ["olive_terraces"] = "29/171/d46f8a175d59dccf 31/171/0f0b67936ea68a16 35/171/48024bc52f4d25e9",
        ["pine_forest"] = "53/18/3856750dc7f51f71 49/18/6b8d058df492107b 47/18/8811cda5be0a88f3",
    };

    [TestCase("wheat_fields"), TestCase("grazing_land"), TestCase("citrus_orchard"), TestCase("olive_terraces"), TestCase("pine_forest")]
    public void BaseTileBuildsAsBefore(string id)
    {
        if (Resources.LoadAll<GameObject>("HexSandbox").Length > 0 || Resources.LoadAll<GameObject>("SettlementPrototypes").Length > 0)
            Assert.Ignore("recorded without the local models; with them the stand-in footprints (#1160) do not apply");
        string now = Fingerprints(id);
        Assert.That(now, Is.EqualTo(Before[id]), $"{id} now builds {now}");
    }
}
