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
        // #1183: 4.4 km hexes, values scale with HexM: wheat_fields re-recorded on the 4.4 km build (same piece counts).
        // The other four are the owner's macOS reference values from the #1796 gate (650049fc merged with main): the Mac is
        // the reference renderer for Unity tests, and this Linux box builds these four differently on main too (platform floats).
        ["wheat_fields"] = "6/23/4f58499d0ba97405 11/21/16b9d119633cacb1 8/23/027e8c11ef672ab7",
        ["grazing_land"] = "22/0/c552715157eff4eb 22/0/6435504967a26520 22/0/5c4e5408808b1357",
        ["citrus_orchard"] = "27/6/7395970659e34854 27/6/85c8c43095a720e3 27/6/7b3ad461fb79d065",
        ["olive_terraces"] = "29/171/f9ac8c9a2a7a4d75 31/171/1ebbe55b00334d7b 35/171/606b1f7d397fbc36",
        ["pine_forest"] = "53/18/bfa95aceafec3572 49/18/bceb425266d54732 47/18/b8be266dd4004335",
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
