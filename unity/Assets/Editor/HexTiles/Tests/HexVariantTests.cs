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

    /// <summary>Recorded on 4841e34c, before the bases were opened for subclassing (FlatSurface, three hexes and seeds).</summary>
    static readonly Dictionary<string, string> Before = new Dictionary<string, string>
    {
        ["wheat_fields"] = "7/23/c963a9126fcda602 13/21/a3a8abb68d2e9e51 10/23/707b2b297d9d6cad",
        ["grazing_land"] = "22/0/b303d42811dfdb4e 22/0/238e7036bd426a72 22/0/9f69bea9b6e841ec",
        ["citrus_orchard"] = "27/6/0a7122b3de0e8c78 27/6/bc917ef820cb84c7 27/6/485aa7a3b49d0fe9",
        ["olive_terraces"] = "31/171/c33d9a14e7606093 32/171/8e2163498c39fc58 35/171/2b99f44e723e1e83",
        ["pine_forest"] = "58/18/4e445ba81e4eb140 53/18/d0fccc37a3e8f5fe 51/18/17fbc0a3f973c6bd",
    };

    [TestCase("wheat_fields"), TestCase("grazing_land"), TestCase("citrus_orchard"), TestCase("olive_terraces"), TestCase("pine_forest")]
    public void BaseTileBuildsAsBefore(string id)
    {
        string now = Fingerprints(id);
        Assert.That(now, Is.EqualTo(Before[id]), $"{id} now builds {now}");
    }
}
