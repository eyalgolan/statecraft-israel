#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.HexTiles;
using Statecraft.HexTiles.Capture;
using UnityEditor;
using UnityEngine;

/// <summary>The character units and the figures the hex tiles draw (Tal, 19 October 2026): a character unit is one model
/// times <see cref="HexUnitScale.Characters"/>, never a mix; every figure a unit or a tile places is whole (no view ray
/// through its clothes, boots or face: HexMeshCheck on the skinned mesh posed as drawn, its idle clip's first frame, as for the buildings); and a squad
/// stands posed (not in its T-pose) at <see cref="HexUnitScale.FigureHeight"/>. Writes the figure measurements (the
/// local proto prefab and the Collection's near and far prefabs of each) to
/// asset-staging/ui-local/hex-tiles/round2/units/figure-report.txt where that local folder exists.</summary>
public class HexUnitTests
{
    /// <summary>The share of view rays a figure may see through, as for the buildings.</summary>
    const float Tolerance = 0.005f;

    static IEnumerable<string> CharacterUnits() =>
        HexUnitTypes.All.Keys.Where(id => HexUnitTypes.Create(id).Create().Members.Any(m => m.StandIn == StandIn.Figure)).OrderBy(k => k, System.StringComparer.Ordinal);

    [TestCaseSource(nameof(CharacterUnits))]
    public void IsOneModelTimesTheCharacterCount(string id)
    {
        var unit = HexUnitTypes.Create(id).Create();
        Assert.That(unit.Members.Count, Is.EqualTo(HexUnitScale.Characters), id);
        Assert.That(unit.Members.Select(m => m.Model).Distinct().ToList(), Has.Count.EqualTo(1), $"{id}: one model per character unit");
    }

    /// <summary>Every human figure the units and tiles place: each character unit's model, and each tile figure that has
    /// a Collection figure (HexModels.FigureAliases). Animals (WheatFields' sheep and donkey) are only reported.</summary>
    static IEnumerable<string> Figures()
    {
        var ids = new SortedSet<string>(System.StringComparer.Ordinal);
        foreach (var id in CharacterUnits()) foreach (var m in HexUnitTypes.Create(id).Create().Members) ids.Add(m.Model);
        var known = new HashSet<string>(HexModels.FigureAliases.Select(a => a.id));
        foreach (var p in AllFigurePlacements()) if (known.Contains(p)) ids.Add(p);
        return ids;
    }

    static IEnumerable<string> AllFigurePlacements()
    {
        foreach (var proto in HexPrototypes.All.Keys)
        {
            var ctx = new HexTileContext(2, -1, new FlatSurface(), 1948);
            HexPrototypes.Create(proto).Build(ctx);
            foreach (var p in ctx.Placements) if (p.StandIn == StandIn.Figure && p.Model != "") yield return p.Model;
        }
    }

    [Test]
    public void FiguresAreWhole()
    {
        WriteReport();
        var holes = new List<string>(); int measured = 0;
        foreach (var id in Figures())
        {
            var prefab = HexModels.Prefab(id);
            if (prefab == null) continue;   // its stand-in on this machine
            var m = HexMeshCheck.OfFigure(prefab); measured++;
            if (m.SeeThroughShare > Tolerance) holes.Add($"{id} ({prefab.name}): {m}");
        }
        Assert.That(measured, Is.GreaterThan(0), "no figure model resolved");
        Assert.That(holes, Is.Empty, "figures that show the ground through them:\n" + string.Join("\n", holes));
    }

    /// <summary>Each Collection figure drawn faces as its proto figure faced (the units and tiles turn figures by that
    /// frame): of the four quarter turns, the proto's vertices lie nearest the drawn figure's as they are, unless another
    /// turn is within a fifth of it. Ignored without the local proto figures.</summary>
    [Test]
    public void CollectionFiguresFaceAsTheirProtoFigures()
    {
        var wrong = new List<string>(); int compared = 0;
        foreach (var (id, _, _) in HexModels.FigureAliases)
        {
            var proto = Resources.Load<GameObject>(HexModels.PathOf(id));
            var drawn = HexModels.Prefab(id);
            if (proto == null || drawn == null || drawn == proto) continue;
            var turns = HexMeshCheck.FigureFacing(proto, drawn).Take(4).ToArray(); compared++;
            int best = System.Array.IndexOf(turns, turns.Min());
            if (best != 0 && turns[best] < 0.8f * turns[0]) wrong.Add($"{id} -> {drawn.name}: turn {HexMeshCheck.FacingNames[best]} {turns[best]:F4} vs as drawn {turns[0]:F4}");
        }
        if (compared == 0) Assert.Ignore("no local proto figures to compare");
        Assert.That(wrong, Is.Empty, string.Join("\n", wrong));
    }

    /// <summary>With the Collection figures off (as in a player, where the Collection streams from bundles) a figure id
    /// draws its local proto prefab again, or nothing (its stand-in).</summary>
    [Test]
    public void WithoutCollectionFiguresTheProtoFigureDraws()
    {
        try
        {
            HexModels.DrawCollectionFigures = false; HexModels.Clear();
            foreach (var (id, _, _) in HexModels.FigureAliases)
                Assert.That(HexModels.Prefab(id), Is.EqualTo(Resources.Load<GameObject>(HexModels.PathOf(id))), id);
        }
        finally { HexModels.DrawCollectionFigures = true; HexModels.Clear(); }
        foreach (var (id, figure, level) in HexModels.FigureAliases)
            Assert.That(HexModels.Prefab(id), Is.EqualTo(AssetDatabase.LoadAssetAtPath<GameObject>(HexModels.FigurePath(figure, level))), id);
    }

    /// <summary>Each squad member, posed standing (HexUnitMotion.Pose, the idle clip's first frame), measured from its
    /// skinned mesh baked in that pose: as tall as HexUnitScale.FigureHeight of the hex (within 15%), and not spread in a
    /// T-pose (its widest side under 0.6 of its height).</summary>
    [TestCaseSource(nameof(CharacterUnits))]
    public void StandsPosedAtFigureHeight(string id)
    {
        var parent = new GameObject("units");
        try
        {
            var motion = new HexUnitMotion(HexUnitTypes.Create(id).Create(), new[] { (0, 0) }, new FlatSurface(), parent.transform);
            motion.Pose(0f, false);
            float want = HexUnitScale.FigureHeight * HexScale.HexM;
            foreach (var go in motion.Objects)
            {
                var size = PosedSize(go);
                Assert.That(size.y, Is.InRange(0.85f * want, 1.15f * want), $"{id} {go.name}: {size.y:F0} m tall, want {want:F0}");
                Assert.That(Mathf.Max(size.x, size.z), Is.LessThan(0.6f * size.y), $"{id} {go.name}: {size.x:F0} x {size.z:F0} m wide for {size.y:F0} m tall (a T-pose?)");
            }
        }
        finally { Object.DestroyImmediate(parent); }
    }

    /// <summary>A placed figure's size in metres in its own frame (x across, z front to back), from its meshes as posed now.</summary>
    static Vector3 PosedSize(GameObject go)
    {
        var inv = Quaternion.Inverse(go.transform.rotation);
        var meshes = HexMeshCheck.WorldMeshes(go);
        try { return HexMeshCheck.SizeOf(meshes.SelectMany(m => m.vertices).Select(v => inv * v)); }
        finally { foreach (var m in meshes) Object.DestroyImmediate(m); }
    }

    static void WriteReport()
    {
        var lines = new List<string>
        {
            "Hex-tile figures, posed as drawn (idle clip, first frame; bind pose in the last two columns). HexMeshCheck: seeThru = view rays that meet the figure but none of",
            "its front faces; the test, within 0.5%. Local proto prefab vs the Collection's near and far prefabs. size = posed",
            "height and widest side in the prefab's units. facing: mean nearest distance of the proto's vertices to the near",
            "prefab's under quarter turns (0 least = same facing). Written by HexUnitTests.",
            "",
            $"{"figure",-58} {"tris",6} {"seeThru",8} {"backFirst",9} {"height",7} {"width",7} {"bind seeThru",13} {"bind h",7}",
        };
        void Row(string name, GameObject? go)
        {
            if (go == null) { lines.Add($"{name,-58} (missing)"); return; }
            var r = HexMeshCheck.OfFigure(go); var s = HexMeshCheck.FigureSize(go);
            var rb = HexMeshCheck.OfFigure(go, false); var sb = HexMeshCheck.FigureSize(go, false);
            lines.Add($"{name,-58} {r.Triangles,6} {r.SeeThroughShare,8:P2} {r.BackFirstShare,9:P2} {s.y,7:F3} {Mathf.Max(s.x, s.z),7:F3} {rb.SeeThroughShare,13:P2} {sb.y,7:F3}");
        }
        foreach (var (id, figure, level) in HexModels.FigureAliases)
        {
            var proto = Resources.Load<GameObject>(HexModels.PathOf(id));
            var near = AssetDatabase.LoadAssetAtPath<GameObject>(HexModels.FigurePath(figure, "near"));
            var far = AssetDatabase.LoadAssetAtPath<GameObject>(HexModels.FigurePath(figure, "far"));
            Row(id + " (local proto)", proto);
            Row(figure + "-near", near);
            Row(figure + "-far", far);
            foreach (var (label, other) in new[] { ("near", near), ("far", far) })
                if (proto != null && other != null)
                {
                    var f = HexMeshCheck.FigureFacing(proto, other);
                    lines.Add($"  facing proto vs {label}: " + string.Join(" ", f.Take(4).Select((v, i) => $"{HexMeshCheck.FacingNames[i]}={v:F4}")));
                }
            var drawn = HexModels.Prefab(id);
            lines.Add($"  drawn now: {(drawn != null ? drawn.name : "stand-in")} (alias level {level})");
            foreach (var (label, go) in new[] { ("proto", proto), ("near", near) })
            {
                if (go == null) continue;
                var inst = Object.Instantiate(go);
                foreach (var s in inst.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    float hCpu = HexMeshCheck.SizeOf(HexMeshCheck.Skinned(s)).y, hBake = HexMeshCheck.SizeOf(HexMeshCheck.Skinned(s, true)).y;
                    lines.Add($"  {label} skin: {s.bones.Length} bones, {s.sharedMesh.boneWeights.Length}/{s.sharedMesh.vertexCount} weights, readable {s.sharedMesh.isReadable}, "
                        + $"height cpu {hCpu:F3} bake {hBake:F3}, root rot {inst.transform.localRotation.eulerAngles}, clips {string.Join("/", inst.GetComponentsInChildren<Animator>().SelectMany(a => a.runtimeAnimatorController != null ? a.runtimeAnimatorController.animationClips.Select(c => c.name) : new string[0]))}");
                }
                Object.DestroyImmediate(inst);
            }
        }
        lines.Add("-- other figures the tiles place (reported only) --");
        var known = new HashSet<string>(HexModels.FigureAliases.Select(a => a.id));
        foreach (var id in AllFigurePlacements().Distinct().Where(i => !known.Contains(i)).OrderBy(i => i, System.StringComparer.Ordinal))
            Row(id, HexModels.Prefab(id));
        var staging = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "asset-staging"));
        var path = Path.Combine(staging, "ui-local/hex-tiles/round2/units/figure-report.txt");
        if (Directory.Exists(staging)) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllLines(path, lines); }
        Debug.Log("HEX FIGURE REPORT" + (Directory.Exists(staging) ? " -> " + path : "") + "\n" + string.Join("\n", lines));
    }
}
